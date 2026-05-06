"use client";

import { useEffect, useRef, useState } from "react";

const CONTACT_EMAIL = process.env.NEXT_PUBLIC_CONTACT_EMAIL || "soporte@odontari.com";
const WHATSAPP_NUMBER = process.env.NEXT_PUBLIC_WHATSAPP || "18091234567";

export default function Contact() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 80)
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
    <section id="contacto" ref={ref} className="py-24 bg-pearl">
      <div className="max-w-6xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-14 items-start">

          {/* Columna izquierda — info */}
          <div>
            <p className="animate-on-scroll section-eyebrow text-teal mb-4">
              Contacto y soporte
            </p>
            <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                           text-3xl sm:text-4xl leading-tight mb-5">
              Estamos aquí para ayudarte
            </h2>
            <p className="animate-on-scroll text-slate text-base leading-relaxed mb-10">
              ¿Tienes preguntas antes de empezar? ¿Necesitas una demo personalizada?
              ¿Algo no funciona como esperabas? Escríbenos y te respondemos a la brevedad.
            </p>

            {/* Canales */}
            <div className="space-y-5">
              <ContactChannel
                icon={<EmailIcon />}
                label="Correo electrónico"
                value={CONTACT_EMAIL}
                href={`mailto:${CONTACT_EMAIL}`}
              />
              <ContactChannel
                icon={<WhatsAppIcon />}
                label="WhatsApp"
                value="+1 (809) 123-4567"
                href={`https://wa.me/${WHATSAPP_NUMBER}?text=Hola,%20me%20interesa%20Odontari`}
              />
              <ContactChannel
                icon={<ClockIcon />}
                label="Horario de soporte"
                value="Lun–Vie 8:00 AM – 6:00 PM (AST)"
                href={null}
              />
            </div>

            {/* Soporte rápido badges */}
            <div className="mt-10 flex flex-wrap gap-3">
              {["Demo gratuita", "Onboarding incluido", "Sin permanencia", "Respuesta en 24h"].map((b) => (
                <span key={b} className="animate-on-scroll px-3.5 py-1.5 rounded-full bg-teal/10
                                         text-teal text-xs font-medium border border-teal/20">
                  {b}
                </span>
              ))}
            </div>
          </div>

          {/* Columna derecha — formulario */}
          <ContactForm />
        </div>
      </div>
    </section>
  );
}

function ContactChannel({
  icon, label, value, href,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  href: string | null;
}) {
  const inner = (
    <div className="animate-on-scroll flex items-center gap-4 group">
      <div className="w-11 h-11 rounded-xl bg-teal/10 flex items-center justify-center
                      text-teal group-hover:bg-teal/20 transition-colors shrink-0">
        {icon}
      </div>
      <div>
        <p className="text-slate text-xs">{label}</p>
        <p className="text-navy-deep font-medium text-sm">{value}</p>
      </div>
    </div>
  );

  return href ? (
    <a href={href} target="_blank" rel="noopener noreferrer">{inner}</a>
  ) : (
    <div>{inner}</div>
  );
}

function ContactForm() {
  const [status, setStatus] = useState<"idle" | "sending" | "sent" | "error">("idle");
  const [form, setForm] = useState({ nombre: "", email: "", clinica: "", mensaje: "" });

  const FORM_ENDPOINT = process.env.NEXT_PUBLIC_FORM_ENDPOINT;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!FORM_ENDPOINT) {
      // Sin endpoint configurado: abre mailto como fallback
      window.location.href = `mailto:${CONTACT_EMAIL}?subject=Consulta desde la web&body=${encodeURIComponent(
        `Nombre: ${form.nombre}\nClínica: ${form.clinica}\nMensaje: ${form.mensaje}`
      )}`;
      return;
    }
    setStatus("sending");
    try {
      const res = await fetch(FORM_ENDPOINT, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(form),
      });
      setStatus(res.ok ? "sent" : "error");
    } catch {
      setStatus("error");
    }
  };

  if (status === "sent") {
    return (
      <div className="animate-on-scroll rounded-2xl bg-teal/10 border border-teal/30 p-10 text-center">
        <div className="w-14 h-14 rounded-full bg-teal/20 flex items-center justify-center mx-auto mb-4">
          <svg className="w-7 h-7 text-teal" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M20 6L9 17l-5-5" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </div>
        <h3 className="text-navy-deep font-bold text-lg mb-2">¡Mensaje recibido!</h3>
        <p className="text-slate text-sm">Te responderemos en menos de 24 horas hábiles.</p>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="animate-on-scroll bg-white rounded-2xl border border-slate/12 shadow-sm p-8 space-y-5"
    >
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
        <FormField
          label="Nombre completo"
          type="text"
          placeholder="Dra. Ana Pérez"
          value={form.nombre}
          onChange={(v) => setForm({ ...form, nombre: v })}
          required
        />
        <FormField
          label="Correo electrónico"
          type="email"
          placeholder="ana@clinica.com"
          value={form.email}
          onChange={(v) => setForm({ ...form, email: v })}
          required
        />
      </div>
      <FormField
        label="Nombre de tu clínica"
        type="text"
        placeholder="Clínica Dental Sonrisa"
        value={form.clinica}
        onChange={(v) => setForm({ ...form, clinica: v })}
      />
      <div>
        <label className="block text-navy-deep/80 text-xs font-medium mb-1.5">Mensaje</label>
        <textarea
          rows={4}
          placeholder="¿En qué podemos ayudarte?"
          value={form.mensaje}
          onChange={(e) => setForm({ ...form, mensaje: e.target.value })}
          required
          className="w-full rounded-xl border border-slate/25 px-4 py-3 text-sm text-navy-deep
                     placeholder:text-slate/50 focus:outline-none focus:border-teal focus:ring-2
                     focus:ring-teal/20 resize-none transition-colors"
        />
      </div>

      <button
        type="submit"
        disabled={status === "sending"}
        className="w-full py-3.5 rounded-xl bg-navy-deep hover:bg-navy-mid text-white font-semibold
                   text-sm transition-all duration-200 disabled:opacity-60 disabled:cursor-not-allowed"
      >
        {status === "sending" ? "Enviando…" : "Enviar mensaje"}
      </button>

      {status === "error" && (
        <p className="text-red-500 text-xs text-center">
          Hubo un error. Escríbenos directamente a {CONTACT_EMAIL}.
        </p>
      )}
    </form>
  );
}

function FormField({
  label, type, placeholder, value, onChange, required,
}: {
  label: string;
  type: string;
  placeholder: string;
  value: string;
  onChange: (v: string) => void;
  required?: boolean;
}) {
  return (
    <div>
      <label className="block text-navy-deep/80 text-xs font-medium mb-1.5">{label}</label>
      <input
        type={type}
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        className="w-full rounded-xl border border-slate/25 px-4 py-3 text-sm text-navy-deep
                   placeholder:text-slate/50 focus:outline-none focus:border-teal focus:ring-2
                   focus:ring-teal/20 transition-colors"
      />
    </div>
  );
}

// Iconos inline
function EmailIcon() {
  return (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <rect x="2" y="4" width="20" height="16" rx="2" />
      <path d="M2 7l10 7 10-7" strokeLinecap="round" />
    </svg>
  );
}
function WhatsAppIcon() {
  return (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347z" />
      <path d="M12 0C5.373 0 0 5.373 0 12c0 2.127.558 4.122 1.532 5.855L.057 23.522a.5.5 0 00.609.61l5.799-1.453A11.944 11.944 0 0012 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.81 9.81 0 01-5.032-1.382l-.361-.214-3.741.937.978-3.637-.235-.374A9.818 9.818 0 012.182 12C2.182 6.57 6.57 2.182 12 2.182c5.43 0 9.818 4.388 9.818 9.818 0 5.43-4.388 9.818-9.818 9.818z" />
    </svg>
  );
}
function ClockIcon() {
  return (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <circle cx="12" cy="12" r="10" />
      <path d="M12 6v6l4 2" strokeLinecap="round" />
    </svg>
  );
}
