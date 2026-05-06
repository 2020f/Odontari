"use client";

import { useEffect, useRef } from "react";

const pains = [
  {
    number: "01",
    title: "Registros dispersos y pérdida de datos",
    description:
      "Historiales en papel, hojas de cálculo y grupos de WhatsApp. La información crítica de tus pacientes se pierde o es difícil de encontrar cuando más la necesitas.",
  },
  {
    number: "02",
    title: "Facturas sin NCF y riesgo fiscal",
    description:
      "Emitir recibos sin comprobante fiscal válido expone tu clínica a sanciones de la DGII. La facturación manual es lenta y propensa a errores.",
  },
  {
    number: "03",
    title: "Sin visibilidad del negocio",
    description:
      "No sabes cuánto facturaste este mes, qué tratamientos son más rentables ni cuántos pacientes no regresaron. Operas a ciegas.",
  },
];

export default function Problem() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 80)
            );
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.08 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <section ref={ref} className="py-24 bg-navy-deep relative overflow-hidden">
      {/* Background detail */}
      <div className="absolute inset-0 pointer-events-none bg-line-grid opacity-60" />
      <div className="absolute top-0 right-0 w-[400px] h-[300px] bg-teal/5 blur-[80px]" />

      <div className="relative z-10 max-w-6xl mx-auto px-6">
        {/* Header — left aligned */}
        <div className="mb-16 max-w-xl">
          <p className="animate-on-scroll section-eyebrow text-teal mb-4">
            El problema
          </p>
          <h2 className="animate-on-scroll font-display font-bold text-white
                         text-3xl sm:text-4xl leading-tight">
            ¿Tu clínica está creciendo,
            <br />pero tu sistema no?
          </h2>
        </div>

        {/* Pain points — vertical list with large number */}
        <div className="space-y-0">
          {pains.map((pain, i) => (
            <div
              key={i}
              className="animate-on-scroll group relative flex gap-8 items-start
                         py-10 border-t border-white/8 last:border-b
                         hover:bg-white/[0.02] transition-colors -mx-6 px-6"
            >
              {/* Large number */}
              <span className="font-display font-extrabold text-[4.5rem] leading-none
                               text-teal/12 group-hover:text-teal/20 transition-colors
                               select-none shrink-0 w-24 text-right mt-1">
                {pain.number}
              </span>

              {/* Content */}
              <div className="flex-1 min-w-0">
                <h3 className="font-display font-bold text-white text-xl mb-3 leading-snug">
                  {pain.title}
                </h3>
                <p className="text-pale/55 text-base leading-relaxed max-w-xl">
                  {pain.description}
                </p>
              </div>

              {/* Right accent line */}
              <div className="hidden lg:block w-px self-stretch bg-gradient-to-b
                              from-transparent via-teal/25 to-transparent shrink-0" />
            </div>
          ))}
        </div>

        {/* Bottom callout */}
        <div className="animate-on-scroll mt-16 flex items-start gap-5 max-w-2xl
                        rounded-2xl border border-teal/20 bg-teal/8 px-7 py-6">
          <div className="w-10 h-10 rounded-xl bg-teal/20 flex items-center justify-center shrink-0 mt-0.5">
            <svg className="w-5 h-5 text-teal-light" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
              <circle cx="12" cy="12" r="10"/>
              <path d="M12 8v5M12 16h.01"/>
            </svg>
          </div>
          <div>
            <p className="text-white font-semibold text-base mb-1">
              El problema no es tu clínica
            </p>
            <p className="text-pale/60 text-sm leading-relaxed">
              Es la falta de un sistema diseñado para cómo realmente trabajas.
              Odontari resuelve exactamente eso.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}
