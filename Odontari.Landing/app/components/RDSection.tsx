"use client";

import { useEffect, useRef } from "react";

const points = [
  {
    title: "NCF automático (DGII)",
    description:
      "Asignación de comprobante fiscal basada en rangos configurados por tipo: consumidor final, crédito fiscal, gubernamental.",
  },
  {
    title: "RNC fiscal por clínica",
    description:
      "Cada clínica configura su RNC y razón social. Las facturas PDF incluyen todos los datos requeridos por la DGII.",
  },
  {
    title: "ITBIS configurable",
    description:
      "Tasa de ITBIS ajustable por clínica. Servicios pueden estar exentos o gravados según su naturaleza.",
  },
  {
    title: "Facturas en PDF instantáneas",
    description:
      "Generación inmediata con NCF, datos fiscales completos, desglose de ITBIS y código de referencia.",
  },
];

export default function RDSection() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 75)
            );
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.1 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <section data-light ref={ref} className="py-24 bg-pearl">
      <div className="max-w-6xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">

          {/* ── Left: copy ── */}
          <div>
            <div className="animate-on-scroll flex items-center gap-2.5 mb-5">
              <div className="w-8 h-8 rounded-lg border border-teal/30 bg-teal/10
                              flex items-center justify-center">
                <svg className="w-4 h-4 text-teal" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
                  <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7z"/>
                  <circle cx="12" cy="9" r="2.5"/>
                </svg>
              </div>
              <p className="section-eyebrow text-teal">
                Hecho para República Dominicana
              </p>
            </div>

            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight mb-6">
              Cumplimiento fiscal
              <br />
              <span className="text-teal">desde el primer día</span>
            </h2>

            <p className="animate-on-scroll text-slate text-base leading-relaxed mb-8 max-w-md">
              Odontari fue diseñado con las regulaciones fiscales dominicanas integradas desde el núcleo.
              La facturación con NCF, los comprobantes de la DGII y el manejo del ITBIS no son configuraciones
              extra — son parte del sistema.
            </p>

            {/* RD flag badge */}
            <div className="animate-on-scroll inline-flex items-center gap-2 px-4 py-2
                            rounded-full border border-slate/20 bg-white shadow-sm">
              <span className="text-base leading-none">🇩🇴</span>
              <span className="text-navy-deep text-xs font-semibold">República Dominicana</span>
              <span className="text-slate/50 text-xs">· Latinoamérica</span>
            </div>
          </div>

          {/* ── Right: fiscal cards ── */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {points.map((pt, i) => (
              <div
                key={i}
                className="animate-on-scroll relative rounded-xl bg-white border border-slate/12
                           p-5 teal-accent-bar pl-8
                           hover:border-teal/30 hover:shadow-sm card-hover transition-all duration-250"
              >
                <h4 className="font-display font-bold text-navy-deep text-sm mb-2">{pt.title}</h4>
                <p className="text-slate text-xs leading-relaxed">{pt.description}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
