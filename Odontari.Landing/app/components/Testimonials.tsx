"use client";

import { useEffect, useRef } from "react";

const testimonials = [
  {
    quote: "Ahora tenemos todo organizado. Antes era un caos con pacientes, citas y cobros en hojas de cálculo distintas. Los pacientes notan la diferencia.",
    name: "Dra. Carolina Mejía",
    role: "Directora",
    clinic: "Clínica Dental Sonrisa, Santo Domingo",
    initials: "CM",
  },
  {
    quote: "El sistema es fácil de usar y muy completo. Mi equipo lo aprendió en un día y ahora la recepción trabaja mucho más ordenada.",
    name: "Dr. Roberto Féliz",
    role: "Odontólogo general",
    clinic: "Consultorio Féliz, Santiago",
    initials: "RF",
  },
  {
    quote: "El consentimiento digital con firma nos ahorra tiempo y nos da tranquilidad legal. Ya no tenemos que guardar papeles físicos.",
    name: "Dra. Paola Vargas",
    role: "Especialista en ortodoncia",
    clinic: "Clínica OrthoSmile",
    initials: "PV",
  },
];

export default function Testimonials() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 90)
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
        <div className="text-center mb-14">
          <p className="animate-on-scroll section-eyebrow text-teal mb-4">
            Testimonios
          </p>
          <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                         text-3xl sm:text-4xl leading-tight">
            Clínicas que ya confían en Odontari
          </h2>
        </div>

        {/* Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {testimonials.map((t, i) => (
            <div
              key={i}
              className="animate-on-scroll flex flex-col bg-white rounded-2xl
                         border border-slate/12 p-7 shadow-sm
                         hover:shadow-md hover:border-teal/25 card-hover transition-all duration-300"
            >
              {/* Quote mark */}
              <svg className="w-8 h-5 text-teal/25 mb-5 shrink-0" viewBox="0 0 32 22" fill="currentColor">
                <path d="M0 22V13.926C0 6.208 4.511 1.851 13.533 0L14.667 2.6C11.556 3.7 9.222 6.067 8.667 9.333H13.333V22H0zm18.667 0V13.926C18.667 6.208 23.178 1.851 32.2 0L33.333 2.6c-3.11 1.1-5.444 3.467-6 6.733H32V22H18.667z" />
              </svg>

              <p className="text-navy-deep/75 text-sm leading-relaxed flex-1 mb-7">
                &ldquo;{t.quote}&rdquo;
              </p>

              {/* Author */}
              <div className="flex items-center gap-3 pt-5 border-t border-slate/10">
                <div className="w-10 h-10 rounded-full bg-navy-deep flex items-center justify-center shrink-0">
                  <span className="font-display font-bold text-white text-xs">{t.initials}</span>
                </div>
                <div>
                  <p className="font-display font-bold text-navy-deep text-sm">{t.name}</p>
                  <p className="text-slate text-[11px]">{t.role} · {t.clinic}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
