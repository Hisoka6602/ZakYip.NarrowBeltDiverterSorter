using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute.Drivers.ZhiQian32Relay;

/// <summary>
/// 智嵌32路网络继电器TCP客户端
/// 负责底层协议封装：构造报文、TCP发送、接收响应
/// </summary>
public sealed class ZhiQian32RelayClient : IDisposable
{
    private readonly ILogger<ZhiQian32RelayClient> _logger;
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly object _lock = new();
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _disposed;

    /// <summary>
    /// 创建智嵌32路网络继电器TCP客户端实例
    /// </summary>
    /// <param name="ipAddress">目标IP地址</param>
    /// <param name="port">TCP端口</param>
    /// <param name="logger">日志记录器</param>
    public ZhiQian32RelayClient(string ipAddress, int port, ILogger<ZhiQian32RelayClient> logger)
    {
        _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        _port = port;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 确保TCP连接已建立
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_tcpClient != null && _tcpClient.Connected && _stream != null)
        {
            return;
        }

        lock (_lock)
        {
            // 清理旧连接
            _stream?.Dispose();
            _tcpClient?.Dispose();
            _stream = null;
            _tcpClient = null;
        }

        try
        {
            _logger.LogInformation(
                "[智嵌继电器客户端] 正在连接到 {IpAddress}:{Port}",
                _ipAddress,
                _port);

            var client = new TcpClient();
            await client.ConnectAsync(_ipAddress, _port, ct);

            lock (_lock)
            {
                _tcpClient = client;
                _stream = client.GetStream();
            }

            _logger.LogInformation(
                "[智嵌继电器客户端] 成功连接到 {IpAddress}:{Port}",
                _ipAddress,
                _port);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[智嵌继电器客户端] 连接失败 {IpAddress}:{Port}",
                _ipAddress,
                _port);
            throw;
        }
    }

    /// <summary>
    /// 设置单个继电器通道状态
    /// </summary>
    /// <param name="channelIndex">通道索引（1..32）</param>
    /// <param name="isOn">是否打开</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetChannelAsync(int channelIndex, bool isOn, CancellationToken ct = default)
    {
        if (channelIndex < 1 || channelIndex > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(channelIndex), "通道索引必须在 1-32 之间");
        }

        await EnsureConnectedAsync(ct);

        try
        {
            // 智嵌32路继电器协议（ASCII模式）
            // 打开继电器：OPEN CH:xx\r\n  其中xx为01-32的两位数字
            // 关闭继电器：CLOSE CH:xx\r\n
            var command = isOn ? "OPEN" : "CLOSE";
            var message = $"{command} CH:{channelIndex:D2}\r\n";
            var bytes = Encoding.ASCII.GetBytes(message);

            _logger.LogDebug(
                "[智嵌继电器客户端] 发送命令: {Command}",
                message.TrimEnd());

            lock (_lock)
            {
                if (_stream == null)
                {
                    throw new InvalidOperationException("网络流未初始化");
                }

                _stream.Write(bytes, 0, bytes.Length);
            }

            // 智嵌继电器通常不返回响应，或返回简单的OK确认
            // 这里我们不等待响应，命令发送即视为成功
            _logger.LogDebug(
                "[智嵌继电器客户端] 命令发送成功: 通道 {ChannelIndex} -> {State}",
                channelIndex,
                isOn ? "开" : "关");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[智嵌继电器客户端] 设置通道 {ChannelIndex} 失败",
                channelIndex);

            // 连接失败时清理连接，下次会重新连接
            lock (_lock)
            {
                _stream?.Dispose();
                _tcpClient?.Dispose();
                _stream = null;
                _tcpClient = null;
            }

            throw;
        }
    }

    /// <summary>
    /// 批量设置所有继电器通道状态
    /// </summary>
    /// <param name="isOn">是否打开</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetAllChannelsAsync(bool isOn, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        try
        {
            // 智嵌32路继电器批量命令（如果支持）
            // 如果不支持批量命令，则循环发送单个通道命令
            var command = isOn ? "OPEN" : "CLOSE";
            var message = $"{command} ALL\r\n";
            var bytes = Encoding.ASCII.GetBytes(message);

            _logger.LogDebug(
                "[智嵌继电器客户端] 发送批量命令: {Command}",
                message.TrimEnd());

            lock (_lock)
            {
                if (_stream == null)
                {
                    throw new InvalidOperationException("网络流未初始化");
                }

                _stream.Write(bytes, 0, bytes.Length);
            }

            _logger.LogDebug(
                "[智嵌继电器客户端] 批量命令发送成功: 所有通道 -> {State}",
                isOn ? "开" : "关");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[智嵌继电器客户端] 批量设置失败");

            // 连接失败时清理连接，下次会重新连接
            lock (_lock)
            {
                _stream?.Dispose();
                _tcpClient?.Dispose();
                _stream = null;
                _tcpClient = null;
            }

            throw;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _stream?.Dispose();
            _tcpClient?.Dispose();
            _stream = null;
            _tcpClient = null;
        }

        _disposed = true;
    }
}
