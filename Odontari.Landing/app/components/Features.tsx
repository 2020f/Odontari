"use client";

import { useEffect, useRef } from "react";

const features = [
  {
    number: "01",
    title: "Agenda inteligente",
    description:
      "Vista dinámica de citas con estados en tiempo real, filtro por doctor y gestión de disponibilidad por franjas horarias.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="4" width="18" height="18" rx="2"/>
        <line x1="16" y1="2" x2="16" y2="6"/>
        <line x1="8" y1="2" x2="8" y2="6"/>
        <line x1="3" y1="10" x2="21" y2="10"/>
        <circle cx="8" cy="15" r="1" fill="currentColor" stroke="none"/>
        <circle cx="12" cy="15" r="1" fill="currentColor" stroke="none"/>
        <circle cx="16" cy="15" r="1" fill="currentColor" stroke="none"/>
      </svg>
    ),
  },
  {
    number: "02",
    title: "Expediente clínico digital",
    description:
      "Odontograma, periodontograma, historial clínico sistémico, archivos y procedimientos en un solo expediente.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2"/>
        <rect x="9" y="3" width="6" height="4" rx="1"/>
        <line x1="9" y1="12" x2="15" y2="12"/>
        <line x1="9" y1="16" x2="13" y2="16"/>
      </svg>
    ),
  },
  {
    number: "03",
    title: "Facturación NCF",
    description:
      "Cumplimiento fiscal dominicano: NCF automático DGII, rangos por tipo de comprobante, facturas PDF con ITBIS.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M4 2l2 2 2-2 2 2 2-2 2 2 2-2v18l-2-2-2 2-2-2-2 2-2-2-2 2V2z"/>
        <line x1="8" y1="10" x2="16" y2="10"/>
        <line x1="8" y1="14" x2="14" y2="14"/>
      </svg>
    ),
  },
  {
    number: "04",
    title: "Consentimiento informado",
    description:
      "Firma digital desde el expediente del paciente. Plantillas configurables, variables automáticas, historial legal inmutable.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M3 17c1-1 2-3 3-4s2 1 3 0 2-3 3-4 2 0 3 1"/>
        <line x1="3" y1="20" x2="21" y2="20"/>
        <path d="M17 8l2-2-2-2-4 4"/>
      </svg>
    ),
  },
  {
    number: "05",
    title: "Reportes y caja",
    description:
      "Historial de pagos, reportes financieros, exportación a Excel y PDF. Control total desde el módulo de caja.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <line x1="18" y1="20" x2="18" y2="10"/>
        <line x1="12" y1="20" x2="12" y2="4"/>
        <line x1="6" y1="20" x2="6" y2="14"/>
        <line x1="2" y1="20" x2="22" y2="20"/>
      </svg>
    ),
  },
  {
    number: "06",
    title: "Multi-usuario y roles",
    description:
      "AdminClinica, Doctor, Recepción y Finanzas — permisos granulares por vista y por usuario. Cada rol ve solo lo que necesita.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
        <circle cx="9" cy="7" r="4"/>
        <path d="M23 21v-2a4 4 0 00-3-3.87"/>
        <path d="M16 3.13a4 4 0 010 7.75"/>
      </svg>
    ),
  },
];

export default function Features() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const items = entry.target.querySelectorAll<HTMLElement>(".animate-on-scroll");
            items.forEach((item, i) => {
              setTimeout(() => item.classList.add("visible"), i * 65);
            });
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
    <section data-light id="features" ref={ref} className="py-24 bg-white">
      <div className="max-w-6xl mx-auto px-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4 mb-16">
          <div>
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Características
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight">
              Todo lo que necesita tu clínica,
              <br className="hidden sm:block" />
              en una sola plataforma
            </h2>
          </div>
          <p className="animate-on-scroll text-slate text-sm leading-relaxed max-w-xs hidden sm:block">
            Módulos diseñados para el flujo real de trabajo de una clínica odontológica.
          </p>
        </div>

        {/* Feature grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-px bg-slate/10 rounded-2xl overflow-hidden
                        border border-slate/10">
          {features.map((feat) => (
            <FeatureCard key={feat.number} {...feat} />
          ))}
        </div>
      </div>
    </section>
  );
}

function FeatureCard({
  number, title, description, icon,
}: {
  number: string;
  title: string;
  description: string;
  icon: React.ReactNode;
}) {
  return (
    <div
      className="animate-on-scroll relative group bg-white p-8
                 hover:bg-pearl transition-colors duration-200 overflow-hidden"
    >
      {/* Large number background */}
      <span className="absolute right-5 top-3 font-display font-extrabold text-[5rem]
                       leading-none text-slate/6 group-hover:text-teal/8 transition-colors
                       select-none pointer-events-none">
        {number}
      </span>

      {/* Icon */}
      <div className="relative w-11 h-11 rounded-xl bg-teal/10 border border-teal/15
                      flex items-center justify-center mb-5 text-teal
                      group-hover:bg-teal/20 group-hover:border-teal/30 transition-colors">
        {icon}
      </div>

      {/* Text */}
      <h3 className="relative font-display font-bold text-navy-deep text-base mb-2.5">{title}</h3>
      <p className="relative text-slate text-sm leading-relaxed">{description}</p>

      {/* Bottom accent line on hover */}
      <div className="absolute bottom-0 left-0 w-0 h-0.5 bg-gradient-to-r from-teal to-sky
                      group-hover:w-full transition-all duration-400 ease-out" />
    </div>
  );
}
