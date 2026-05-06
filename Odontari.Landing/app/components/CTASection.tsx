"use client";

import { useEffect, useRef } from "react";

const APP_URL = process.env.NEXT_PUBLIC_APP_URL || "http://localhost:5000";
const WHATSAPP = process.env.NEXT_PUBLIC_WHATSAPP || "18091234567";

export default function CTASection() {
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
      { threshold: 0.2 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <section
      ref={ref}
      className="relative py-28 bg-navy-deep overflow-hidden"
    >
      {/* Background detail */}
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute inset-0 bg-dot-grid opacity-30" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2
                        w-[700px] h-[400px] bg-teal/10 blur-[100px] rounded-full" />
        <div className="absolute top-0 left-0 right-0 h-px
                        bg-gradient-to-r from-transparent via-teal/30 to-transparent" />
        <div className="absolute bottom-0 left-0 right-0 h-px
                        bg-gradient-to-r from-transparent via-teal/30 to-transparent" />
      </div>

      <div className="relative z-10 max-w-3xl mx-auto px-6 text-center">
        <p className="animate-on-scroll section-eyebrow text-teal mb-6">
          Empieza hoy
        </p>
        <h2 className="animate-on-scroll font-display font-bold text-white
                       text-4xl sm:text-5xl leading-tight mb-5">
          Lleva tu clínica
          <br />
          <span className="text-gradient-sky">al siguiente nivel</span>
        </h2>
        <p className="animate-on-scroll text-pale/60 text-lg mb-10 max-w-lg mx-auto">
          Únete a las clínicas que ya gestionan su operación con Odontari.
          Sin contratos. Sin complicaciones.
        </p>

        <div className="animate-on-scroll flex flex-col sm:flex-row items-center justify-center gap-4">
          <a
            href={`${APP_URL}/Identity/Account/Login`}
            className="inline-flex items-center gap-2 px-8 py-4 rounded-xl
                       bg-teal hover:bg-teal-light text-white font-semibold text-base
                       shadow-[0_8px_32px_rgba(78,142,162,0.4)]
                       hover:shadow-[0_8px_40px_rgba(78,142,162,0.55)]
                       transition-all duration-200 hover:-translate-y-0.5"
          >
            Comenzar gratis
            <svg className="w-4 h-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M3 8h10M9 4l4 4-4 4" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </a>
          <a
            href={`https://wa.me/${WHATSAPP}?text=Hola%2C%20quiero%20una%20demo%20de%20ODONTARI`}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-8 py-4 rounded-xl
                       border border-white/20 text-white/80 font-medium text-base
                       hover:bg-white/8 hover:text-white hover:border-white/35
                       transition-all duration-200"
          >
            Solicitar demo
          </a>
        </div>

        {/* Small trust indicators */}
        <div className="animate-on-scroll mt-10 flex flex-wrap items-center justify-center gap-6">
          {["Demo gratuita", "Sin tarjeta de crédito", "Soporte en español"].map((t) => (
            <div key={t} className="flex items-center gap-2 text-pale/40">
              <svg className="w-3 h-3 text-teal/60" viewBox="0 0 12 12" fill="none"
                   stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                <path d="M2 6l3 3 5-5" />
              </svg>
              <span className="text-xs">{t}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
