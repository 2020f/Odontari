"use client";

import { useEffect, useRef } from "react";

const modules = [
  { label: "Pacientes", sub: "Expediente completo", color: "text-teal-light", bg: "bg-teal/15 border-teal/25" },
  { label: "Agenda",    sub: "Citas y disponibilidad", color: "text-sky",       bg: "bg-sky/10 border-sky/20" },
  { label: "Historial", sub: "Clínico sistémico",     color: "text-pale/80",   bg: "bg-white/8 border-white/12" },
  { label: "Pagos",     sub: "Cobros y facturas",     color: "text-teal-light", bg: "bg-teal/10 border-teal/20" },
  { label: "Consentim.", sub: "Firma digital",          color: "text-sky",        bg: "bg-sky/8 border-sky/15" },
  { label: "Reportes",  sub: "Excel y PDF",           color: "text-pale/80",   bg: "bg-white/5 border-white/10" },
] as const;

const benefits = [
  "Control total de pacientes y expedientes",
  "Agenda inteligente por doctor",
  "Historial clínico sistémico completo",
  "Gestión de pagos con NCF integrado",
  "Consentimientos digitales con firma",
  "Acceso desde cualquier dispositivo",
];

export default function Solution() {
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
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              La solución
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight mb-5">
              Todo lo que necesita
              <br />tu clínica,{" "}
              <span className="relative inline-block">
                unificado
                <span className="absolute bottom-1 left-0 w-full h-[3px] bg-teal/40 rounded-full" />
              </span>
            </h2>
            <p className="animate-on-scroll text-slate text-base leading-relaxed mb-10 max-w-md">
              ODONTARI centraliza todas las operaciones clínicas y administrativas en una
              plataforma moderna, segura y fácil de usar. Sin instalaciones, desde el primer día.
            </p>

            {/* Benefit list */}
            <ul className="space-y-3.5">
              {benefits.map((b, i) => (
                <li key={i}
                    className="animate-on-scroll flex items-center gap-3.5">
                  <span className="w-5 h-5 rounded-full bg-teal flex items-center justify-center shrink-0">
                    <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="none"
                         stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M2 6l3 3 5-5" />
                    </svg>
                  </span>
                  <span className="text-navy-deep/80 text-sm font-medium">{b}</span>
                </li>
              ))}
            </ul>
          </div>

          {/* ── Right: visual ── */}
          <div className="animate-on-scroll">
            <div className="relative rounded-2xl bg-navy-deep border border-white/10
                            shadow-[0_24px_60px_rgba(0,29,57,0.3)] overflow-hidden">
              {/* Window chrome */}
              <div className="px-5 py-3 border-b border-white/8 flex items-center gap-2">
                {["#ff5f56","#ffbd2e","#27c93f"].map(c => (
                  <div key={c} className="w-3 h-3 rounded-full" style={{ background: c, opacity: 0.7 }} />
                ))}
                <span className="ml-2 text-pale/35 text-xs font-mono">Panel principal</span>
              </div>

              {/* Module grid */}
              <div className="p-5 grid grid-cols-3 gap-3">
                {modules.map((m) => (
                  <div key={m.label}
                       className={`rounded-xl border p-4 flex flex-col gap-1.5 ${m.bg}`}>
                    <p className={`text-xs font-semibold ${m.color}`}>{m.label}</p>
                    <p className="text-pale/40 text-[10px]">{m.sub}</p>
                  </div>
                ))}
              </div>

              {/* Stats bar */}
              <div className="mx-5 mb-5 rounded-xl border border-white/8 bg-white/5
                              grid grid-cols-3 divide-x divide-white/8">
                {[
                  { value: "248", label: "Pacientes", color: "text-white" },
                  { value: "12", label: "Citas hoy", color: "text-white" },
                  { value: "RD$84K", label: "Este mes", color: "text-teal-light" },
                ].map((s) => (
                  <div key={s.label} className="py-4 text-center">
                    <p className={`font-display font-bold text-lg ${s.color}`}>{s.value}</p>
                    <p className="text-pale/40 text-[10px]">{s.label}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
