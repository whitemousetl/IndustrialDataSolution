using HslCommunication;
using HslCommunication.Core;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;

namespace IndustrialDataProcessor.Infrastructure.Communication.Extensions;

public static class HslReadWriteExtensions
{
    public static async Task<PointResult> ReadPointAsync(this IReadWriteNet device, ParameterConfig point)
    {
        if (!point.DataType.HasValue) throw new InvalidOperationException("未指定数据类型");

        var result = new PointResult
        {
            Address = point.Address,
            Label = point.Label,
            DataType = point.DataType.Value
        };

        // 统一在这里处理所有数据类型的分发
        var (isSuccess, content, message) = point.DataType.Value switch
        {
            DataType.Bool => Wrap(await device.ReadBoolAsync(point.Address)),
            DataType.Short => Wrap(await device.ReadInt16Async(point.Address)),
            DataType.UShort => Wrap(await device.ReadUInt16Async(point.Address)),
            DataType.Int => Wrap(await device.ReadInt32Async(point.Address)),
            DataType.UInt => Wrap(await device.ReadUInt32Async(point.Address)),
            DataType.Long => Wrap(await device.ReadInt64Async(point.Address)),
            DataType.ULong => Wrap(await device.ReadUInt64Async(point.Address)),
            DataType.Float => Wrap(await device.ReadFloatAsync(point.Address)),
            DataType.Double => Wrap(await device.ReadDoubleAsync(point.Address)),
            DataType.String => Wrap(await device.ReadStringAsync(point.Address, point.Length)),
            _ => throw new NotSupportedException($"不支持的数据类型: {point.DataType}")
        };

        result.ReadIsSuccess = isSuccess;
        result.Value = isSuccess ? content : null;
        result.ErrorMsg = isSuccess ? string.Empty : message;

        return result;
    }

    public static async Task<bool> WritePointAsync(this IReadWriteNet device, ParameterConfig point, object value)
    {
        if (!point.DataType.HasValue)
            throw new InvalidOperationException("未指定数据类型");

        if (value == null)
            throw new ArgumentNullException(nameof(value), "写入值不能为 null");

        string address = point.Address;

        OperateResult result;
        try
        {
            result = point.DataType.Value switch
            {
                DataType.Bool => await device.WriteAsync(address, Convert.ToBoolean(value)),
                DataType.Short => await device.WriteAsync(address, Convert.ToInt16(value)),
                DataType.UShort => await device.WriteAsync(address, Convert.ToUInt16(value)),
                DataType.Int => await device.WriteAsync(address, Convert.ToInt32(value)),
                DataType.UInt => await device.WriteAsync(address, Convert.ToUInt32(value)),
                DataType.Long => await device.WriteAsync(address, Convert.ToInt64(value)),
                DataType.ULong => await device.WriteAsync(address, Convert.ToUInt64(value)),
                DataType.Float => await device.WriteAsync(address, Convert.ToSingle(value)),
                DataType.Double => await device.WriteAsync(address, Convert.ToDouble(value)),
                DataType.String => await device.WriteAsync(address, Convert.ToString(value)),
                _ => throw new NotSupportedException($"不支持的写入数据类型: {point.DataType}")
            };
        }
        catch (InvalidCastException ex)
        {
            throw new InvalidOperationException($"无法将值 '{value}' 转换为类型 {point.DataType}", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"值 '{value}' 的格式不正确，无法转换为 {point.DataType}", ex);
        }
        catch (OverflowException ex)
        {
            throw new InvalidOperationException($"值 '{value}' 超出 {point.DataType} 的范围", ex);
        }

        if (!result.IsSuccess)
            throw new Exception(result.Message);

        return result.IsSuccess;
    }

    private static (bool IsSuccess, object? Content, string Message) Wrap<T>(OperateResult<T> res)
    {
        return (res.IsSuccess, res.Content, res.Message);
    }
}
