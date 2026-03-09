using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using MediatR;
using System.Text.Json;

namespace IndustrialDataProcessor.Application.Commands;

public record SaveWorkstationConfigCommand(WorkstationConfigDto dto) : IRequest;

