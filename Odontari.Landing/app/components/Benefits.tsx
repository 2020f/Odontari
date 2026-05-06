"use client";

import { useEffect, useRef } from "react";

const stats = [
  { value: "100%", label: "En la nube", sub: "Sin instalaciones locales" },
  { value: "+6",   label: "Módulos integrados", sub: "Agenda, expediente, caja y más" },
  { value: "NCF",  label: "Cumplimiento DGII", sub: "Facturación fiscal nativa" },
  { value: "24/7", label: "Datos respaldados", sub: "Azure Cloud — alta disponibilidad" },
];

const benefits = [
  {
    title: "Profesionaliza tu clínica",
    description: "Deja atrás el manejo improvisado. Opera con procesos claros y documentados desde el día uno.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="3" width="20" height="14" rx="2"/>
        <path d="M8 21h8M12 17v4"/>
      </svg>
    ),
  },
  {
    title: "Ahorra tiempo del equipo",
    description: "Menos procesos manuales, más eficiencia. Tu equipo hace más en menos tiempo.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="12" r="10"/>
        <polyline points="12 6 12 12 16 14"/>
      </svg>
    ),
  },
  {
    title: "Seguridad y respaldo",
    description: "Tu información protegida en Azure Cloud. Respaldo automático, acceso por roles, arquitectura multi-tenant.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
      </svg>
    ),
  },
  {
    title: "Toma decisiones con datos",
    description: "Reportes en tiempo real. No más suposiciones — números reales para hacer crecer tu negocio.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <line x1="18" y1="20" x2="18" y2="10"/>
        <line x1="12" y1="20" x2="12" y2="4"/>
        <line x1="6" y1="20" x2="6" y2="14"/>
        <line x1="2" y1="20" x2="22" y2="20"/>
      </svg>
    ),
  },
  {
    title: "Cumplimiento legal",
    description: "Consentimientos firmados digitalmente con registro inmutable. Protege tu clínica ante cualquier reclamación.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M9 11l3 3L22 4"/>
        <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/>
      </svg>
    ),
  },
  {
    title: "Soporte cercano",
    description: "No estás solo. Onboarding, capacitación y asistencia técnica incluidos. Soporte en español.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
        <circle cx="9" cy="7" r="4"/>
        <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75"/>
      </svg>
    ),
  },
];

export default function Benefits() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 55)
            );
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.06 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <section ref={ref} data-light className="bg-white">
      {/* ── Stats bar ── */}
      <div className="bg-navy-deep border-t border-white/8">
        <div className="max-w-6xl mx-auto px-6 py-12">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-0 divide-y divide-white/8
                          lg:divide-y-0 lg:divide-x">
            {stats.map((s) => (
              <div key={s.value}
                   className="animate-on-scroll text-center py-6 lg:py-0 lg:px-8 first:pl-0 last:pr-0">
                <p className="font-display font-extrabold text-[2.5rem] leading-none
                               text-gradient-sky mb-1.5">
                  {s.value}
                </p>
                <p className="text-white font-semibold text-sm mb-1">{s.label}</p>
                <p className="text-pale/40 text-xs">{s.sub}</p>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* ── Benefits grid ── */}
      <div className="max-w-6xl mx-auto px-6 py-24">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-14">
          <div>
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Beneficios
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight">
              ¿Por qué elegir Odontari?
            </h2>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-7">
          {benefits.map((b, i) => (
            <div
              key={i}
              className="animate-on-scroll group flex gap-4 p-6 rounded-2xl
                         border border-slate/12 bg-white hover:border-teal/30 hover:bg-pearl
                         card-hover transition-colors duration-250"
            >
              <div className="w-10 h-10 rounded-xl bg-teal/10 border border-teal/15
                              flex items-center justify-center text-teal shrink-0 mt-0.5
                              group-hover:bg-teal/20 group-hover:border-teal/30 transition-colors">
                {b.icon}
              </div>
              <div>
                <h3 className="font-display font-bold text-navy-deep text-sm mb-2">{b.title}</h3>
                <p className="text-slate text-sm leading-relaxed">{b.description}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
