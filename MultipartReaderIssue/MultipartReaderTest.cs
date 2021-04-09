using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;

using Xunit;

namespace MultipartReaderIssue
{
    public class MultipartReaderTest
    {
        private static async Task DoTest(
            bool removeQuotes)
        {
            var builder = new WebHostBuilder()
                .Configure(appBuilder => {
                    appBuilder.Run(async context => {

                        var body = context.Request.Body;

                        var mediaType =
                            MediaTypeHeaderValue.Parse(context.Request.ContentType);

                        var boundary = mediaType.Parameters
                            .Single(x =>
                                string.Equals(x.Name, "boundary", StringComparison.OrdinalIgnoreCase))
                            .Value;

                        if (removeQuotes)
                        {
                            boundary = boundary.Trim('"');
                        }

                        var reader = new MultipartReader(boundary, body);

                        await reader
                            .ReadNextSectionAsync()
                            .ConfigureAwait(false);
                    });
                });

            using var server = new TestServer(builder);

            using var client = server.CreateClient();

            using var part = new StringContent("hello");
            part.Headers.ContentType =
                MediaTypeHeaderValue.Parse("text/plain; charset=utf-8");

            using var requestContent = new MultipartContent("mixed");
            requestContent.Add(part);

            using var request = new HttpRequestMessage(HttpMethod.Post, "http://example");
            request.Content = requestContent;

            using var response = await client
                .SendAsync(request)
                .ConfigureAwait(false);

            Assert.Equal(200, (int)response.StatusCode);
        }

        [Fact]
        public Task TestWithRemoveQuotes()
        {
            return DoTest(true);
        }

        [Fact]
        public Task TestWithoutRemoveQuotes()
        {
            return DoTest(false);
        }
    }
}
