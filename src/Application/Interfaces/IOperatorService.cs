using Application.Common;
using Domain.Entities;

namespace Application.Interfaces;

public interface IOperatorService
{
    OperationResult<ParkingSession> CheckIn(Guid operatorUserId, Guid userId, Guid vehicleId, string slotCode);
    OperationResult<Payment> CheckOut(Guid operatorUserId, Guid sessionId);
}
