"use client";

import { useEffect, useRef } from "react";

const points = [
  {
    title: "Base de datos segura",
    description: "SQL Server en la nube con cifrado en reposo y en tránsito. Nunca datos sin proteger.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <rect x="5" y="11" width="14" height="11" rx="2"/>
        <path d="M7 11V7a5 5 0 0110 0v4"/>
      </svg>
    ),
  },
  {
    title: "Accesos por rol",
    description: "Cada usuario solo ve lo que le corresponde. Permisos granulares por vista y por usuario.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
        <path d="M9 12l2 2 4-4"/>
      </svg>
    ),
  },
  {
    title: "Respaldo en la nube",
    description: "Archivos en Azure Blob Storage. Tu información nunca se pierde, accesible cuando la necesites.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M18 10h-1.26A8 8 0 109 20h9a5 5 0 000-10z"/>
      </svg>
    ),
  },
  {
    title: "Aislamiento multi-tenant",
    description: "Los datos de tu clínica están completamente separados de otras clínicas. ClinicaId en cada registro.",
    icon: (
      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="3" width="7" height="7" rx="1"/>
        <rect x="14" y="3" width="7" height="7" rx="1"/>
        <rect x="3" y="14" width="7" height="7" rx="1"/>
        <rect x="14" y="14" width="7" height="7" rx="1"/>
      </svg>
    ),
  },
];

export default function Security() {
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
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-14">
          <div>
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Seguridad
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight">
              Tu información está protegida
            </h2>
          </div>
          <p className="animate-on-scroll text-slate text-sm max-w-xs hidden sm:block">
            Tomamos la seguridad en serio. Tu clínica nos confía datos sensibles y
            nos aseguramos de estar a la altura.
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          {points.map((p, i) => (
            <div
              key={i}
              className="animate-on-scroll group p-7 rounded-2xl bg-white border border-slate/12
                         shadow-sm hover:border-teal/30 hover:shadow-md card-hover
                         transition-all duration-300"
            >
              <div className="w-11 h-11 rounded-xl bg-teal/10 border border-teal/15
                              flex items-center justify-center text-teal mb-5
                              group-hover:bg-teal/20 transition-colors">
                {p.icon}
              </div>
              <h3 className="font-display font-bold text-navy-deep text-sm mb-2">{p.title}</h3>
              <p className="text-slate text-xs leading-relaxed">{p.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
