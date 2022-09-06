// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SparkplugNode.cs" company="Hämmer Electronics">
// The project is licensed under the MIT license.
// </copyright>
// <summary>
//   Defines the SparkplugNode type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SparkplugNet.VersionB;

using SparkplugNet.Core;

/// <inheritdoc cref="SparkplugNodeBase{T}"/>
public class SparkplugNode : SparkplugNodeBase<VersionBData.Metric>
{
    /// <inheritdoc cref="SparkplugNodeBase{T}"/>
    /// <summary>
    /// Initializes a new instance of the <see cref="SparkplugNode"/> class.
    /// </summary>
    /// <param name="knownMetrics">The known metrics.</param>
    /// <param name="logger">The logger.</param>
    public SparkplugNode(IEnumerable<VersionBData.Metric> knownMetrics, ILogger? logger = null) : base(knownMetrics, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SparkplugNode"/> class.
    /// </summary>
    /// <param name="knownMetricsStorage">The metric names.</param>
    /// <param name="logger">The logger.</param>
    /// /// <seealso cref="SparkplugNodeBase{T}"/>
    public SparkplugNode(KnownMetricStorage knownMetricsStorage, ILogger? logger = null) : base(knownMetricsStorage, logger)
    {
    }

    /// <summary>
    /// Publishes version B metrics for a node.
    /// </summary>
    /// <param name="metrics">The metrics.</param>
    /// <exception cref="ArgumentNullException">The options are null.</exception>
    /// <exception cref="Exception">An invalid metric type was specified.</exception>
    /// <returns>A <see cref="MqttClientPublishResult"/>.</returns>
    protected override async Task<MqttClientPublishResult> PublishMessage(IEnumerable<VersionBData.Metric> metrics)
    {
        if (this.Options is null)
        {
            throw new ArgumentNullException(nameof(this.Options), "The options aren't set properly.");
        }

        if (this.KnownMetrics is null)
        {
            throw new ArgumentNullException(nameof(this.KnownMetrics), "The KnownMetrics aren't set properly.");
        }

        // Get the data message.
        var dataMessage = this.MessageGenerator.GetSparkPlugNodeDataMessage(
            this.NameSpace,
            this.Options.GroupIdentifier,
            this.Options.EdgeNodeIdentifier,
            this.KnownMetricsStorage.FilterOutgoingMetrics(metrics),
            this.LastSequenceNumber,
            this.LastSessionNumber,
            DateTimeOffset.Now,
            this.Options.AddSessionNumberToDataMessages);

        // Debug output.
        this.Logger?.Debug("NDATA Message: {@DataMessage}", dataMessage);

        // Increment the sequence number.
        this.IncrementLastSequenceNumber();

        // Publish the message.
        return await this.Client.PublishAsync(dataMessage);
    }

    /// <summary>
    /// Called when [message received].
    /// </summary>
    /// <param name="topic">The topic.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>
    /// A <see cref="T:System.Threading.Tasks.Task" /> representing any asynchronous operation.
    /// </returns>
    /// <exception cref="System.InvalidCastException">The metric cast didn't work properly.</exception>
    protected override async Task OnMessageReceived(string topic, byte[] payload)
    {
        var payloadVersionB = PayloadHelper.Deserialize<VersionBProtoBuf.ProtoBufPayload>(payload);

        if (payloadVersionB is not null)
        {
            var convertedPayload = PayloadConverter.ConvertVersionBPayload(payloadVersionB);

            if (topic.Contains(SparkplugMessageType.DeviceCommand.GetDescription()))
            {
                if (convertedPayload is not VersionBData.Payload convertedPayloadVersionB)
                {
                    throw new InvalidCastException("The metric cast didn't work properly.");
                }

                foreach (var metric in convertedPayloadVersionB.Metrics)
                {
                    if (metric is VersionBData.Metric convertedMetric)
                    {
                        await this.FireDeviceCommandReceivedAsync(convertedMetric);
                    }
                }
            }

            if (topic.Contains(SparkplugMessageType.NodeCommand.GetDescription()))
            {
                if (convertedPayload is not VersionBData.Payload convertedPayloadVersionB)
                {
                    throw new InvalidCastException("The metric cast didn't work properly.");
                }

                foreach (var metric in convertedPayloadVersionB.Metrics)
                {
                    if (metric is VersionBData.Metric convertedMetric)
                    {
                        await this.FireNodeCommandReceivedAsync(convertedMetric);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Publishes version B metrics for a device.
    /// </summary>
    /// <param name="metrics">The metrics.</param>
    /// <param name="deviceIdentifier">The device identifier.</param>
    /// <exception cref="ArgumentNullException">The options are null.</exception>
    /// <exception cref="Exception">The device is unknown or an invalid metric type was specified.</exception>
    /// <returns>A <see cref="MqttClientPublishResult"/>.</returns>
    protected override async Task<MqttClientPublishResult> PublishMessageForDevice(IEnumerable<VersionBData.Metric> metrics, string deviceIdentifier)
    {
        if (this.Options is null)
        {
            throw new ArgumentNullException(nameof(this.Options), "The options aren't set properly.");
        }

        if (!this.KnownDevices.ContainsKey(deviceIdentifier))
        {
            throw new Exception("The device is unknown, please publish a device birth message first.");
        }

        var deviceMetrics = this.KnownDevices[deviceIdentifier];

        if (deviceMetrics is null)
        {
            throw new ArgumentNullException(deviceIdentifier, $"The KnownMetrics for the device {deviceIdentifier} aren't set properly.");
        }

        // Get the data message.
        var dataMessage = this.MessageGenerator.GetSparkPlugDeviceDataMessage(
            this.NameSpace,
            this.Options.GroupIdentifier,
            this.Options.EdgeNodeIdentifier,
            deviceIdentifier,
            this.KnownMetricsStorage.FilterOutgoingMetrics(metrics),
            this.LastSequenceNumber,
            this.LastSessionNumber,
            DateTimeOffset.Now,
            this.Options.AddSessionNumberToDataMessages);

        // Increment the sequence number.
        this.IncrementLastSequenceNumber();

        // Publish the message.
        return await this.Client.PublishAsync(dataMessage);
    }
}
