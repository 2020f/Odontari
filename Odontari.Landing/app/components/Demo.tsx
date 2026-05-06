"use client";

import { useEffect, useRef } from "react";

const WHATSAPP = process.env.NEXT_PUBLIC_WHATSAPP || "18091234567";
const CONTACT_EMAIL = process.env.NEXT_PUBLIC_CONTACT_EMAIL || "soporte@odontari.com";

const perks = [
  { title: "Demo en vivo", sub: "Con un especialista" },
  { title: "Acceso temporal", sub: "Datos de ejemplo reales" },
  { title: "Sin compromiso", sub: "Cancela cuando quieras" },
  { title: "Respuestas", sub: "A todas tus dudas" },
];

export default function Demo() {
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
      { threshold: 0.15 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <section id="demo" ref={ref} className="py-24 bg-white">
      <div className="max-w-5xl mx-auto px-6">
        <div className="relative rounded-2xl bg-navy-deep overflow-hidden
                        border border-white/8 shadow-[0_24px_80px_rgba(0,29,57,0.2)]">
          {/* Top accent line */}
          <div className="absolute top-0 left-0 right-0 h-0.5
                          bg-gradient-to-r from-teal via-sky to-teal" />
          {/* Background detail */}
          <div className="absolute inset-0 pointer-events-none bg-line-grid" />
          <div className="absolute right-0 top-0 w-72 h-72 bg-teal/8 blur-[80px]" />

          <div className="relative z-10 px-10 sm:px-16 py-14 text-center">
            <p className="animate-on-scroll section-eyebrow text-teal mb-5">
              Demo gratuita
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-white
                           text-3xl sm:text-4xl leading-tight mb-4">
              Pruébalo antes de decidir
            </h2>
            <p className="animate-on-scroll text-pale/60 text-base max-w-xl mx-auto mb-10">
              Agenda una demostración personalizada y descubre cómo Odontari
              puede transformar la gestión de tu clínica.
            </p>

            {/* Perks */}
            <div className="animate-on-scroll grid grid-cols-2 sm:grid-cols-4 gap-4 mb-10 max-w-2xl mx-auto">
              {perks.map((p, i) => (
                <div key={i} className="rounded-xl border border-white/8 bg-white/5 p-4">
                  <p className="text-white font-display font-semibold text-sm mb-0.5">{p.title}</p>
                  <p className="text-pale/50 text-xs">{p.sub}</p>
                </div>
              ))}
            </div>

            {/* CTAs */}
            <div className="animate-on-scroll flex flex-col sm:flex-row items-center justify-center gap-4">
              <a
                href={`https://wa.me/${WHATSAPP}?text=Hola%2C%20quiero%20solicitar%20una%20demo%20de%20ODONTARI`}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-2 px-8 py-3.5 rounded-xl
                           bg-teal hover:bg-teal-light text-white font-semibold text-sm
                           shadow-[0_8px_24px_rgba(78,142,162,0.35)]
                           transition-all duration-200 hover:-translate-y-0.5"
              >
                <svg className="w-4.5 h-4.5" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347zM12 0C5.373 0 0 5.373 0 12c0 2.127.558 4.122 1.532 5.855L.057 23.522a.5.5 0 00.609.61l5.799-1.453A11.944 11.944 0 0012 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.81 9.81 0 01-5.032-1.382l-.361-.214-3.741.937.978-3.637-.235-.374A9.818 9.818 0 012.182 12C2.182 6.57 6.57 2.182 12 2.182c5.43 0 9.818 4.388 9.818 9.818 0 5.43-4.388 9.818-9.818 9.818z" />
                </svg>
                Solicitar demo por WhatsApp
              </a>
              <a
                href={`mailto:${CONTACT_EMAIL}?subject=Solicitud de demo ODONTARI`}
                className="inline-flex items-center gap-2 px-8 py-3.5 rounded-xl
                           border border-white/20 text-white/80 font-medium text-sm
                           hover:bg-white/8 hover:text-white transition-all duration-200"
              >
                Solicitar por email
              </a>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
