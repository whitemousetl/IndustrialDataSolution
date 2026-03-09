using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Application.Dtos;

public record SaveWorkstationConfigRequest(
    [Required(ErrorMessage = "配置内容不能为空")]
    [property: JsonPropertyName("json_content")]
    JsonElement JsonContent
);
