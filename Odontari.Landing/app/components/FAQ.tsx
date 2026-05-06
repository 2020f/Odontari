"use client";

import { useEffect, useRef, useState } from "react";

const faqs = [
  {
    q: "¿Necesito instalar algo?",
    a: "No. ODONTARI es 100% en la nube. Solo necesitas un navegador web y conexión a internet. Sin instalaciones, sin descargas, sin mantenimiento de servidor.",
  },
  {
    q: "¿Puedo usarlo desde el celular?",
    a: "Sí. La plataforma es completamente responsiva y funciona desde cualquier navegador moderno — computadora, tablet o teléfono.",
  },
  {
    q: "¿Qué pasa con la seguridad de mis datos?",
    a: "Los datos de tu clínica están completamente aislados (arquitectura multi-tenant). Los archivos se almacenan en Azure Blob Storage con acceso autenticado. Nunca compartimos información entre clínicas.",
  },
  {
    q: "¿Puedo cancelar cuando quiera?",
    a: "Sí. Puedes cancelar en cualquier momento sin penalizaciones. Tendrás acceso hasta el final del período pagado y podemos exportar tus datos antes de que expire.",
  },
  {
    q: "¿Se adapta al flujo de mi clínica?",
    a: "Sí. Puedes configurar doctores, servicios, plantillas de consentimiento, rangos NCF y permisos por usuario. El sistema se ajusta al flujo de trabajo de tu clínica.",
  },
  {
    q: "¿Cumple con la DGII para facturación?",
    a: "Sí. ODONTARI gestiona rangos de NCF por tipo (consumidor final, crédito fiscal, gubernamental). Cada factura incluye RNC, razón social, NCF y desglose de ITBIS según la normativa vigente.",
  },
  {
    q: "¿Puedo tener varios doctores en la misma cuenta?",
    a: "Sí. Los planes Profesional y Premium soportan múltiples usuarios con roles diferenciados: AdminClinica, Doctor, Recepción y Finanzas.",
  },
  {
    q: "¿Ofrecen demo o período de prueba?",
    a: "Sí. Contáctanos por WhatsApp o email y te habilitamos acceso a una demo con datos de ejemplo para que explores el sistema sin compromiso.",
  },
];

export default function FAQ() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 50)
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
    <section id="faq" ref={ref} className="py-24 bg-white">
      <div className="max-w-6xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_2fr] gap-16">
          {/* Left — sticky header */}
          <div className="lg:sticky lg:top-28 lg:self-start">
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Preguntas frecuentes
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight mb-5">
              Todo lo que necesitas saber
            </h2>
            <p className="animate-on-scroll text-slate text-sm leading-relaxed">
              ¿No encuentras tu respuesta? Escríbenos directamente y te ayudamos.
            </p>
          </div>

          {/* Right — accordion */}
          <div className="space-y-2">
            {faqs.map((faq, i) => (
              <FAQItem key={i} q={faq.q} a={faq.a} />
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

function FAQItem({ q, a }: { q: string; a: string }) {
  const [open, setOpen] = useState(false);

  return (
    <div
      className={`animate-on-scroll rounded-xl border overflow-hidden transition-all duration-200
        ${open
          ? "border-teal/30 bg-pearl"
          : "border-slate/12 bg-white hover:border-slate/25"
        }`}
    >
      <button
        onClick={() => setOpen(!open)}
        className="w-full flex items-center justify-between gap-4 px-6 py-4.5 text-left"
      >
        <span className={`font-display font-semibold text-sm leading-snug
                          ${open ? "text-navy-deep" : "text-navy-deep/80"}`}>
          {q}
        </span>
        <div className={`w-6 h-6 rounded-full flex items-center justify-center shrink-0
                         transition-colors ${open ? "bg-teal text-white" : "bg-slate/10 text-slate"}`}>
          <svg className={`w-3 h-3 transition-transform duration-200 ${open ? "rotate-45" : ""}`}
               viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2">
            <line x1="6" y1="1" x2="6" y2="11" />
            <line x1="1" y1="6" x2="11" y2="6" />
          </svg>
        </div>
      </button>
      {open && (
        <div className="px-6 pb-5">
          <p className="text-slate text-sm leading-relaxed">{a}</p>
        </div>
      )}
    </div>
  );
}
