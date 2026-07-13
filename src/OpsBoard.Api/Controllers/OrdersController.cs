using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Api.Auth;
using OpsBoard.Application.Common;
using OpsBoard.Application.Orders;
using OpsBoard.Infrastructure.Services;

namespace OpsBoard.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Policy = AuthorizationPolicies.OrdersManage)]
public sealed class OrdersController : ApiControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var response = await _orderService.GetActiveAsync(cancellationToken);
        return JsonOk(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orderService.GetByIdAsync(id, cancellationToken);
            return JsonOk(response);
        }
        catch (AppException exception)
        {
            return JsonError(exception);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orderService.CreateAsync(request, cancellationToken);
            return JsonCreated(response, $"/api/orders/{response.Id}");
        }
        catch (AppException exception)
        {
            return JsonError(exception);
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orderService.UpdateStatusAsync(id, request, cancellationToken);
            return JsonOk(response);
        }
        catch (AppException exception)
        {
            return JsonError(exception);
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orderService.CancelAsync(id, cancellationToken);
            return JsonOk(response);
        }
        catch (AppException exception)
        {
            return JsonError(exception);
        }
    }
}
