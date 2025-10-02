// Service/MinIOService.cs
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Notification; // EventType, NotificationInfo, Event
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using vision_tool_client_wpf.Settings;


namespace vision_tool_client_wpf.Service
{
    /// <summary>
    /// MinIO 연결/검증 + 목록/실시간 알림(저비용) + 드문 리컨실 지원 서비스.
    /// - API 호출 최소화를 위해: 실시간 스트림(1개) + (옵션) 희귀 리컨실만 사용
    /// </summary>
    public sealed class MinIOService : IDisposable
    {
        private readonly MinIOConfig _cfg;
        private IMinioClient? _client;

        // 로컬 인덱스(키 → (Size, LastModified))  — 화면/동기화를 위한 캐시
        private readonly ConcurrentDictionary<string, (long? Size, DateTime? LastModified)> _index = new();

        // 실시간 스트림 관리
        private CancellationTokenSource? _realtimeCts;
        private IDisposable? _realtimeSubscription;

        // 드문 리컨실(전체 스캔) 관리
        private CancellationTokenSource? _reconcileCts;

        public MinIOService(MinIOConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            if (_client != null) return;

            if (string.IsNullOrWhiteSpace(_cfg.ServiceUrl))
                throw new InvalidOperationException("ServiceUrl이 비어있습니다.");
            if (string.IsNullOrWhiteSpace(_cfg.AccessKey) || string.IsNullOrWhiteSpace(_cfg.SecretKey))
                throw new InvalidOperationException("AccessKey/SecretKey가 비어있습니다.");

            _client = new MinioClient()
                .WithEndpoint(new Uri(_cfg.ServiceUrl).Host) // "https://host[:port]"
                .WithCredentials(_cfg.AccessKey, _cfg.SecretKey)
                .WithSSL(_cfg.UseSSL)
                .Build();

            if (!string.IsNullOrWhiteSpace(_cfg.BucketName))
            {
                bool exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_cfg.BucketName), ct);
                if (!exists)
                    throw new InvalidOperationException($"Bucket '{_cfg.BucketName}' 이(가) 존재하지 않습니다.");
            }
            else
            {
                // 버킷이 지정되지 않은 경우에도 연결/인증 검증을 위해 호출
                await _client.ListBucketsAsync(cancellationToken: ct);
            }
        }

        /// <summary>
        /// (선택) 초기 1회 전체 스냅샷을 불러와 로컬 인덱스 구성.
        /// 비용을 더 아끼려면 생략 가능(이벤트로만 점진 반영).
        /// </summary>
        public async Task SeedIndexOnceAsync(string? prefix = null, bool recursive = true, CancellationToken ct = default)
        {
            EnsureConnected();
            if (string.IsNullOrWhiteSpace(_cfg.BucketName))
                throw new InvalidOperationException("기본 Bucket이 설정되어 있지 않습니다.");

            // ListObjectsAsync는 IObservable<Item>을 반환 (★ await 금지)
            var observable = _client!.ListObjectsAsync(
                new ListObjectsArgs()
                    .WithBucket(_cfg.BucketName)
                    .WithPrefix(prefix ?? string.Empty)
                    .WithRecursive(recursive)
            );

            //var observable = _client!.ListObjectsAsync(
            //    new ListObjectsArgs()
            //        .WithBucket(_cfg.BucketName)
            //        .WithPrefix(prefix ?? string.Empty)
            //        .WithRecursive(recursive)
            //);

            var list = await CollectAsync(observable, ct);
            foreach (var i in list)
            {
                _index[i.Key] = (ToLongNullable(i.Size), i.LastModifiedDateTime);
            }
        }

        /// <summary>
        /// 실시간 알림(버킷 이벤트) 구독: API 호출 거의 없이 변경만 반영.
        /// onDelta: 증분 변경분(배치)을 UI 등에 전달.
        /// </summary>
        public void StartRealtimeAsync(Func<IReadOnlyList<ObjectDelta>, Task> onDelta, string? prefix = null)
        {
            EnsureConnected();
            if (string.IsNullOrWhiteSpace(_cfg.BucketName))
                throw new InvalidOperationException("기본 Bucket이 설정되어 있지 않습니다.");

            StopRealtime(); // 중복 방지

            _realtimeCts = new CancellationTokenSource();
            var token = _realtimeCts.Token;

            // 이벤트 배치(디바운스)
            var buffer = new List<ObjectDelta>();
            var sync = new object();
            async Task FlushAsync()
            {
                List<ObjectDelta> toSend;
                lock (sync)
                {
                    if (buffer.Count == 0) return;
                    toSend = new List<ObjectDelta>(buffer);
                    buffer.Clear();
                }
                await onDelta(toSend);
            }

            // Listen → IObservable<NotificationInfo>
            var observable = _client!.ListenBucketNotificationsAsync(
                new ListenBucketNotificationsArgs()
                    .WithBucket(_cfg.BucketName)
                    .WithPrefix(prefix ?? string.Empty)
                    .WithSuffix("") // 예: ".png" 등 확장자 필터 가능
                    .WithEvents(new List<EventType>
                    {
                EventType.ObjectCreatedAll,
                EventType.ObjectRemovedAll
                    })
            );

            // 구독 (OnNext / OnError / OnCompleted)
            _realtimeSubscription = observable.Subscribe(
                onNext: notification =>
                {
                    var payload = notification.Json;
                    if (string.IsNullOrWhiteSpace(payload)) return;

                    try
                    {
                        // === 1) XML 응답 (ListBucketResult) ===
                        if (payload.AsSpan().TrimStart().StartsWith("<"))
                        {
                            // XML을 목록으로 간주: 모두 AddedOrUpdated로 반영
                            // Namespace 유무 모두 대응
                            var x = XDocument.Parse(payload);

                            XName contentsName;
                            if (x.Root is null)
                                return;

                            // 네임스페이스 판단
                            var ns = x.Root.GetDefaultNamespace();
                            contentsName = ns == XNamespace.None ? "Contents" : ns + "Contents";

                            var items = x.Root.Elements(contentsName);
                            foreach (var c in items)
                            {
                                // 요소 이름들
                                XName Key = ns == XNamespace.None ? "Key" : ns + "Key";
                                XName Size = ns == XNamespace.None ? "Size" : ns + "Size";
                                XName LastModified = ns == XNamespace.None ? "LastModified" : ns + "LastModified";
                                XName ETag = ns == XNamespace.None ? "ETag" : ns + "ETag";

                                var key = (string?)c.Element(Key);
                                if (string.IsNullOrEmpty(key)) continue;

                                var sizeVal = (string?)c.Element(Size);
                                long size = 0;
                                _ = long.TryParse(sizeVal, out size);

                                var lmVal = (string?)c.Element(LastModified);
                                DateTime? lm = null;
                                if (!string.IsNullOrEmpty(lmVal) && DateTime.TryParse(lmVal, out var t)) lm = t;

                                // ETag가 HTML 엔티티일 수 있어 디코딩(선택)
                                var etag = WebUtility.HtmlDecode((string?)c.Element(ETag) ?? "");

                                _index[key] = (size, lm);
                                lock (sync) buffer.Add(ObjectDelta.AddedOrUpdated(key, size, lm));
                            }

                            _ = Task.Delay(200, token).ContinueWith(_ => FlushAsync(), token,
                                TaskContinuationOptions.None, TaskScheduler.Default);
                            return;
                        }

                        // === 2) JSON 응답 (정상 알림 이벤트) ===
                        using var doc = JsonDocument.Parse(payload);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("Records", out var records) && records.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var rec in records.EnumerateArray())
                            {
                                var evt = rec.TryGetProperty("eventName", out var en) ? en.GetString() : null;

                                DateTime? time = null;
                                if (rec.TryGetProperty("eventTime", out var et) &&
                                    et.ValueKind == JsonValueKind.String &&
                                    DateTime.TryParse(et.GetString(), out var tt))
                                    time = tt;

                                string? key = null;
                                long? size = null;
                                if (rec.TryGetProperty("s3", out var s3) &&
                                    s3.TryGetProperty("object", out var obj))
                                {
                                    if (obj.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String)
                                        key = k.GetString();

                                    if (obj.TryGetProperty("size", out var sz) &&
                                        sz.ValueKind == JsonValueKind.Number &&
                                        sz.TryGetInt64(out var lsz))
                                        size = lsz;
                                }

                                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(evt))
                                    continue;

                                if (evt.StartsWith("s3:ObjectCreated", StringComparison.Ordinal))
                                {
                                    _index[key] = (size, time);
                                    lock (sync) buffer.Add(ObjectDelta.AddedOrUpdated(key, size, time));
                                }
                                else if (evt.StartsWith("s3:ObjectRemoved", StringComparison.Ordinal))
                                {
                                    _index.TryRemove(key, out _);
                                    lock (sync) buffer.Add(ObjectDelta.Removed(key));
                                }
                            }

                            _ = Task.Delay(200, token).ContinueWith(_ => FlushAsync(), token,
                                TaskContinuationOptions.None, TaskScheduler.Default);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MinIO Listen][Parse] {ex.Message}");
                    }
                },
                onError: err =>
                {
                    System.Diagnostics.Debug.WriteLine($"[MinIO Listen] {err}");
                },
                onCompleted: () =>
                {
                    System.Diagnostics.Debug.WriteLine("[MinIO Listen] Completed");
                }
            );
        }


        /// <summary>
        /// (아주 드물게) 전체 재검증. 예: 30~60분에 1회 정도.
        /// </summary>
        public void StartRareReconcileAsync(
            Func<IReadOnlyList<ObjectDelta>, Task> onDelta,
            TimeSpan interval,
            string? prefix = null,
            bool recursive = true)
        {
            EnsureConnected();
            if (string.IsNullOrWhiteSpace(_cfg.BucketName))
                throw new InvalidOperationException("기본 Bucket이 설정되어 있지 않습니다.");

            StopReconcile();

            _reconcileCts = new CancellationTokenSource();
            var token = _reconcileCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var obs = _client!.ListObjectsAsync(
                            new ListObjectsArgs()
                                .WithBucket(_cfg.BucketName)
                                .WithPrefix(prefix ?? string.Empty)
                                .WithRecursive(recursive)
                        );

                        var list = await CollectAsync(obs, token);
                        var latest = list.ToDictionary(
                            i => i.Key,
                            i => (ToLongNullable(i.Size), i.LastModifiedDateTime));

                        var deltas = Diff(_index, latest);

                        // 로컬 인덱스 갱신
                        foreach (var d in deltas)
                        {
                            if (d.Kind == DeltaKind.Removed) _index.TryRemove(d.Key, out _);
                            else _index[d.Key] = (d.Size, d.LastModified);
                        }

                        if (deltas.Count > 0)
                            await onDelta(deltas);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MinIO Reconcile] {ex.Message}");
                    }

                    await Task.Delay(interval, token);
                }
            }, token);
        }

        public void StopRealtime()
        {
            try { _realtimeSubscription?.Dispose(); } catch { /* ignore */ }
            _realtimeSubscription = null;

            try { _realtimeCts?.Cancel(); } catch { /* ignore */ }
            _realtimeCts = null;
        }

        public void StopReconcile()
        {
            try { _reconcileCts?.Cancel(); } catch { /* ignore */ }
            _reconcileCts = null;
        }

        public IReadOnlyList<ObjectDescriptor> Snapshot()
        {
            return _index.Select(kv => new ObjectDescriptor(kv.Key, kv.Value.Size, kv.Value.LastModified))
                         .OrderBy(d => d.Key)
                         .ToList();
        }

        private void EnsureConnected()
        {
            if (_client is null)
                throw new InvalidOperationException("아직 MinIO에 연결되지 않았습니다. ConnectAsync를 먼저 호출하세요.");
        }

        /// <summary>
        /// IObservable<Item>을 전부 수집해서 List<Item>으로 반환
        /// </summary>
        private static Task<List<Item>> CollectAsync(IObservable<Item> source, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<List<Item>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var bag = new List<Item>();

            var disp = source.Subscribe(
                onNext: i => bag.Add(i),
                onError: ex => tcs.TrySetException(ex),
                onCompleted: () => tcs.TrySetResult(bag)
            );

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    try { disp.Dispose(); } catch { /* ignore */ }
                    tcs.TrySetCanceled(ct);
                });
            }
            return tcs.Task;
        }

        /// <summary>
        /// current(로컬 인덱스) vs latest(전체 스캔 결과) → 증분 계산
        /// </summary>
        private static List<ObjectDelta> Diff(
            ConcurrentDictionary<string, (long? Size, DateTime? LastModified)> current,
            Dictionary<string, (long? Size, DateTime? LastModified)> latest)
        {
            var deltas = new List<ObjectDelta>();

            // 추가/변경
            foreach (var kv in latest)
            {
                if (!current.TryGetValue(kv.Key, out var old) ||
                    old.Size != kv.Value.Size ||
                    old.LastModified != kv.Value.LastModified)
                {
                    deltas.Add(ObjectDelta.AddedOrUpdated(kv.Key, kv.Value.Size, kv.Value.LastModified));
                }
            }
            // 삭제
            foreach (var kv in current)
            {
                if (!latest.ContainsKey(kv.Key))
                    deltas.Add(ObjectDelta.Removed(kv.Key));
            }

            return deltas;
        }

        private static long? ToLongNullable(ulong? v)
            => v.HasValue ? checked((long)v.Value) : null;

        private static long? ToLongNullable(ulong v)
            => checked((long)v);

        public void Dispose()
        {
            StopRealtime();
            StopReconcile();
        }
    }

    /// <summary>UI 목록 등에 쓰기 좋은 최소 DTO</summary>
    public sealed record ObjectDescriptor(string Key, long? Size, DateTime? LastModified);

    public enum DeltaKind { AddedOrUpdated, Removed }

    public sealed record ObjectDelta(DeltaKind Kind, string Key, long? Size, DateTime? LastModified)
    {
        public static ObjectDelta AddedOrUpdated(string key, long? size, DateTime? lastModified)
            => new(DeltaKind.AddedOrUpdated, key, size, lastModified);

        public static ObjectDelta Removed(string key)
            => new(DeltaKind.Removed, key, null, null);
    }
}
