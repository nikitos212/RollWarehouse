using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace RollWarehouse.Api
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionMiddleware(RequestDelegate next) => _next = next;
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex switch
                {
                    ArgumentException _ => StatusCodes.Status400BadRequest,
                    KeyNotFoundException _ => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                };
                var res = JsonSerializer.Serialize(new { error = ex.Message });
                await context.Response.WriteAsync(res);
            }
        }
    }
}
