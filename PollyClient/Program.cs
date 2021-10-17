using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Polly;
using Polly.Timeout;

namespace ResilienciaComPolly
{
    public static class Program
    {
        static void Main(string[] args)
        {
            // Retry().GetAwaiter().GetResult();
            // RetryStatusCodeNotSucess().GetAwaiter().GetResult();
            // RetryStatusCodeUnauthorized().GetAwaiter().GetResult();
            // Timeout().GetAwaiter().GetResult();
            CircuitBreaker().GetAwaiter().GetResult();
            Console.ReadKey();
        }

        static async Task Retry()
        {
            const int QUANTIDADE_DE_TENTATIVAS = 5;

            var policy = Policy.Handle<HttpRequestException>(e => e.StatusCode != HttpStatusCode.OK)
                .WaitAndRetryAsync(QUANTIDADE_DE_TENTATIVAS,
                    i => TimeSpan.FromSeconds(1 + i),
                    onRetry: async (exception, timeSpan) =>
                        {
                            //  Executa o código após cada falha
                            Console.WriteLine($"\nExceção =>> {exception}\n");
                            Console.WriteLine($"\nTempo =>> {timeSpan}\n");
                            await Task.Delay(0);
                        });

            await policy.ExecuteAsync(async () =>
                {
                    Console.WriteLine("Tentando executar");
                    await Request("retry");
                });
        }

        static async Task RetryStatusCodeNotSucess()
        {
            const int QUANTIDADE_DE_TENTATIVAS = 5;

            var policy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(QUANTIDADE_DE_TENTATIVAS, i => TimeSpan.FromSeconds(1 + i));

            await policy.ExecuteAsync(() => Request("admin"));
        }

        static async Task RetryStatusCodeUnauthorized()
        {
            const int QUANTIDADE_DE_TENTATIVAS = 5;
            const HttpStatusCode unauthorized = HttpStatusCode.Unauthorized;

            var policy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == unauthorized)
            .RetryAsync(QUANTIDADE_DE_TENTATIVAS, async (response, retryCount) =>
            {
                // Refresh Token

                // Adicione sua lógica
                await Task.Delay(2000);
                Console.WriteLine($"{response.Result} | {retryCount}");
                var ex = new HttpRequestException("Não autorizado", null, response.Result.StatusCode);
                if (retryCount == 5) throw ex;
            });

            await policy.ExecuteAsync(() => Request("admin"));
        }

        static async Task Timeout()
        {
            const int TEMPO_MAXIMO = 5;

            var policy = Policy.TimeoutAsync(
                TimeSpan.FromSeconds(TEMPO_MAXIMO),
                TimeoutStrategy.Pessimistic,
                onTimeoutAsync: async (ctx, TimeSpan, task) =>
                {
                    await Task.Delay(0);
                    Console.WriteLine("Timeout ocorrido");
                });

            await policy.ExecuteAsync(async () =>
            {
                Console.WriteLine("Tentando executar");
                await Request("timeout");
            });
        }

        static async Task CircuitBreaker()
        {
            var circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
            onBreak: (exception, timeSpan) =>
            {
                // lógica para quando entra na falha
                Console.WriteLine("Circuito entrou em estado de falha");
            },
            onReset: () =>
            {
                // lógica quando sai da falha
                Console.WriteLine("Circuito saiu do estado de falha");
            });

            var retryPolicy = Policy
            .Handle<Exception>()
            .RetryForeverAsync();

            await retryPolicy.WrapAsync(circuitBreaker)
                .ExecuteAsync(async () => await Request("circuitBreaker", CancellationToken.None));
        }

        static async Task<HttpResponseMessage> Request(string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            const string BASE_URL = "http://localhost:5000";

            Console.WriteLine($"Request para {url}");
            var client = new HttpClient();
            var response = await client.GetAsync($"{BASE_URL}/api/polly/{url}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return response;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<string[]>(json);
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine(new string('-', 50));
            Console.WriteLine();

            return response;
        }
    }
}
