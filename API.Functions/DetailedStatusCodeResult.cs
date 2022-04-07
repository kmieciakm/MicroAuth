using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace API.Functions;

public class DetailedStatusCodeResult : ActionResult
{
    public int StatusCode { get; }
    public object Details { get; set; }

    public DetailedStatusCodeResult(int statusCode, object details)
    {
        StatusCode = statusCode;
        Details = details;
    }

    public override void ExecuteResult(ActionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        context.HttpContext.Response.StatusCode = StatusCode;

        string body = System.Text.Json.JsonSerializer.Serialize(Details);
        context.HttpContext.Response.WriteAsync(body);
    }
}