"use client";

import { useEffect, useRef } from "react";

const values = [
  {
    title: "Tecnología + práctica real",
    description: "Construido por personas que entienden cómo operan las clínicas día a día.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="3" width="20" height="14" rx="2"/>
        <path d="M8 21h8M12 17v4"/>
      </svg>
    ),
  },
  {
    title: "Simplicidad + potencia",
    description: "Interfaz limpia y flujos intuitivos sin sacrificar funcionalidades avanzadas.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>
      </svg>
    ),
  },
  {
    title: "Crecimiento del cliente",
    description: "Tu éxito es nuestro éxito. Evolucionamos el sistema según las necesidades reales.",
    icon: (
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/>
        <polyline points="17 6 23 6 23 12"/>
      </svg>
    ),
  },
];

export default function About() {
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
      { threshold: 0.1 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <section data-light ref={ref} className="py-24 bg-white">
      <div className="max-w-6xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">

          {/* Copy */}
          <div>
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Sobre nosotros
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight mb-5">
              Construido por quienes
              <br />
              <span className="text-teal">entienden tu clínica</span>
            </h2>
            <p className="animate-on-scroll text-slate text-base leading-relaxed mb-4">
              ODONTARI nace con el objetivo de transformar la forma en que las clínicas
              odontológicas gestionan su información. Vimos de cerca los problemas reales
              de la operación diaria y construimos una solución que los resuelve.
            </p>
            <p className="animate-on-scroll text-slate text-base leading-relaxed">
              Desarrollado con tecnología moderna, pensado para la realidad de clínicas en
              crecimiento — especialmente en República Dominicana — con visión hacia toda
              Latinoamérica.
            </p>
          </div>

          {/* Values */}
          <div className="space-y-4">
            {values.map((v, i) => (
              <div
                key={i}
                className="animate-on-scroll flex items-start gap-4 p-6 rounded-2xl
                           border border-slate/12 bg-white hover:border-teal/30 hover:bg-pearl
                           card-hover transition-all duration-250"
              >
                <div className="w-10 h-10 rounded-xl bg-teal/10 border border-teal/15
                                flex items-center justify-center text-teal shrink-0 mt-0.5">
                  {v.icon}
                </div>
                <div>
                  <h3 className="font-display font-bold text-navy-deep text-sm mb-1.5">{v.title}</h3>
                  <p className="text-slate text-sm leading-relaxed">{v.description}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
