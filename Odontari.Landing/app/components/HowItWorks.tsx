"use client";

import { useEffect, useRef } from "react";

const steps = [
  { number: "01", title: "Registras tu clínica", description: "Configura tu clínica con RNC, razón social, doctores y servicios en minutos." },
  { number: "02", title: "Configuras tu equipo", description: "Agrega usuarios con sus roles: AdminClinica, Doctor, Recepción, Finanzas." },
  { number: "03", title: "Empiezas a agendar", description: "Agenda citas por doctor, controla disponibilidad y gestiona confirmaciones." },
  { number: "04", title: "Atiendes pacientes", description: "El doctor documenta procedimientos, odontograma y firma el consentimiento." },
  { number: "05", title: "Registras pagos", description: "Genera la orden de cobro y emite la factura con NCF automático en PDF." },
  { number: "06", title: "Control total", description: "Reportes, historial financiero y analítica para tomar mejores decisiones." },
];

export default function HowItWorks() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 85)
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
    <section ref={ref} id="how-it-works" className="py-24 bg-navy-deep relative overflow-hidden">
      <div className="absolute inset-0 pointer-events-none bg-line-grid" />

      <div className="relative z-10 max-w-6xl mx-auto px-6">
        {/* Header */}
        <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-4 mb-16">
          <div>
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Cómo funciona
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-white
                           text-3xl sm:text-4xl leading-tight">
              Empieza a operar en minutos
            </h2>
          </div>
          <p className="animate-on-scroll text-pale/50 text-sm max-w-xs hidden lg:block">
            Sin semanas de implementación. Sin consultores externos. Solo configura y opera.
          </p>
        </div>

        {/* Steps grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {steps.map((step, i) => (
            <div
              key={i}
              className="animate-on-scroll group relative rounded-xl border border-white/8
                         bg-white/[0.03] hover:bg-white/[0.07] p-7
                         hover:border-teal/25 transition-all duration-300"
            >
              {/* Step number — large, faint */}
              <span className="absolute right-5 top-3 font-display font-extrabold text-[4.5rem]
                               leading-none text-white/5 group-hover:text-teal/10 transition-colors
                               select-none pointer-events-none">
                {step.number}
              </span>

              {/* Number badge */}
              <div className="relative w-10 h-10 rounded-xl border border-teal/30
                              bg-teal/15 flex items-center justify-center mb-5
                              group-hover:bg-teal/25 transition-colors">
                <span className="font-display font-bold text-teal-light text-xs">{step.number}</span>
              </div>

              <h3 className="relative font-display font-bold text-white text-base mb-2">
                {step.title}
              </h3>
              <p className="relative text-pale/55 text-sm leading-relaxed">
                {step.description}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
