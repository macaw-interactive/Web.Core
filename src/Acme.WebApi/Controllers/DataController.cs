﻿using Acme.Core.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using Web.Core.Configuration;
using Web.Core.Mvc;
using Web.Core.WebApi.Controllers;

namespace Acme.WebApi.Controllers
{
    /// <summary>
    /// Controller to produce some test data and senario's for the WebApi.Core
    /// </summary>
    [ApiController]
    [ApiVersion("1", Deprecated = true)]
    [Route("api/v{version:apiVersion}/data")]
    public class DataController : ApiControllerBase
    {
        private readonly ILogger<DataController> _logger;
        private readonly IConfigurationValidator _configValidator;
        private readonly AcmeSettings _acmeSettings;

        public DataController(ILogger<DataController> logger, IConfigurationValidator configValidator, IOptionsSnapshot<AcmeSettings> options)
        {
            _logger = logger;
            _configValidator = configValidator;
            _acmeSettings = options?.Value;
        }

        /// <summary>
        /// Minimal sample implementation of named backend-to-backend service.
        /// </summary>
        /// <remarks>
        /// Supported arguments: 0 is default value and the Ok case, 1..3 give errors.
        /// 
        /// * Ok result - returns array of two strings
        /// * 1: 500 - InternalServerErrror, some exception
        /// * 2: 400 - BadRequest with title
        /// * 3: 404 - NotFound
        /// </remarks>
        /// <param name="value">A value 0..3 for different error conditions.</param>
        /// <returns>An array with two sample strings.</returns>
        [HttpGet("")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExceptionProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult Data(int value = 0)
        {
            switch (value)
            {
                case 1:
                    throw new ArgumentException("some exception", new ArgumentOutOfRangeException("some param", "another inner exception"));

                case 2:
                    return BadRequest("some title", "more details");

                case 3:
                    return NotFound();
            }

            return Ok(new string[] {
                "Value 1 from Test.WebApp.WebApi version 1",
                "Value 2 from Test.WebApp.WebApi version 1"
            });
        }

        /// <summary>
        /// Displays all Amce settings and a summary of all validation errors (if any)
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>An array of strings with configuration information.</returns>
        [HttpGet("settings")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExceptionProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult Settings()
        {
            var output = new List<string>() {
                "= Acme Settings ================",
                $"BackgroundColor: {_acmeSettings.BackgroundColor}",
                $"FontColor: {_acmeSettings.FontColor}",
                $"FontSize: {_acmeSettings.FontSize}",
                $"Message: {_acmeSettings.Message}",
                $"SomethingImportant: {_acmeSettings.SomethingImportant}",
            };

            var errors = _configValidator.Validate();
            if (errors.Any())
            {
                output.Add("= Validation Errors ============");
                output.AddRange(errors);
            }

            output.Add("= All Settings =================");
            output.AddRange(_configValidator.GetAllSettings());

            return Ok(output);
        }
    }
}
