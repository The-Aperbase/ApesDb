import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { resourceFromAttributes } from "@opentelemetry/resources";
import { BatchSpanProcessor } from "@opentelemetry/sdk-trace-base";
import { WebTracerProvider } from "@opentelemetry/sdk-trace-web";
import {
  ATTR_DEPLOYMENT_ENVIRONMENT_NAME,
  ATTR_SERVICE_NAME,
  ATTR_SERVICE_NAMESPACE,
} from "@opentelemetry/semantic-conventions";

export function registerTelemetry() {
  const resource = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: "apesdb-web",
    [ATTR_SERVICE_NAMESPACE]: "apesdb",
    [ATTR_DEPLOYMENT_ENVIRONMENT_NAME]: import.meta.env.DEV ? "development" : "production",
  });
  const exporter = new OTLPTraceExporter({ url: "/otlp/v1/traces" });
  const provider = new WebTracerProvider({
    resource,
    spanProcessors: [new BatchSpanProcessor(exporter)],
  });

  provider.register();
  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new FetchInstrumentation({
        ignoreUrls: [/\/otlp\/v1\/traces$/],
        propagateTraceHeaderCorsUrls: [window.location.origin],
      }),
    ],
  });
}
