"use client";

import { useEffect, useRef } from "react";

const roadmap = [
  { label: "App móvil nativa", status: "Próximamente", color: "bg-teal/20 text-teal-light border-teal/25" },
  { label: "Integración con WhatsApp", status: "Próximamente", color: "bg-teal/20 text-teal-light border-teal/25" },
  { label: "IA para gestión clínica", status: "En desarrollo", color: "bg-sky/20 text-sky border-sky/25" },
  { label: "Automatización de citas", status: "En desarrollo", color: "bg-sky/20 text-sky border-sky/25" },
  { label: "Multi-sucursal", status: "Planificado", color: "bg-white/8 text-pale/60 border-white/12" },
  { label: "Expansión Latinoamérica", status: "Planificado", color: "bg-white/8 text-pale/60 border-white/12" },
];

export default function FutureVision() {
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
    <section ref={ref} className="py-24 bg-navy-deep relative overflow-hidden">
      <div className="absolute inset-0 pointer-events-none bg-line-grid" />
      <div className="absolute bottom-0 right-0 w-80 h-60 bg-sky/5 blur-[80px]" />

      <div className="relative z-10 max-w-6xl mx-auto px-6">
        {/* Header */}
        <div className="flex flex-col lg:flex-row lg:items-end justify-between gap-4 mb-14">
          <div>
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Lo que viene
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-white
                           text-3xl sm:text-4xl leading-tight">
              El futuro de tu clínica
            </h2>
          </div>
          <p className="animate-on-scroll text-pale/45 text-sm max-w-xs hidden lg:block">
            ODONTARI está en constante evolución para llevar tu clínica al siguiente nivel.
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {roadmap.map((item, i) => (
            <div
              key={i}
              className="animate-on-scroll flex items-center justify-between gap-4 p-5
                         rounded-xl border border-white/8 bg-white/[0.04]
                         hover:bg-white/[0.07] hover:border-white/15 transition-colors"
            >
              <p className="font-display font-semibold text-white text-sm">{item.label}</p>
              <span className={`px-2.5 py-1 rounded-full text-[10px] font-bold shrink-0
                               border ${item.color}`}>
                {item.status}
              </span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
