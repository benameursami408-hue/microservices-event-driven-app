import { useState } from 'react';
import { Button, Input, Modal, Select, Textarea } from '../ui';

export function EntityForm({ title, initialValues = {}, fields, onSubmit, onClose, submitLabel = 'Save' }) {
  const [form, setForm] = useState(initialValues);

  function update(name, value) {
    setForm(current => ({ ...current, [name]: value }));
  }

  function submit(event) {
    event.preventDefault();
    onSubmit(form);
  }

  return (
    <Modal
      title={title}
      onClose={onClose}
      footer={(
        <>
          <Button type="button" onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="primary" form="entity-form">{submitLabel}</Button>
        </>
      )}
    >
      <form id="entity-form" className="form-grid" onSubmit={submit}>
        {fields.map(field => {
          const value = form[field.name] ?? field.defaultValue ?? '';
          if (field.type === 'textarea') {
            return (
              <Textarea
                key={field.name}
                label={field.label}
                rows={field.rows || 4}
                className={field.full ? 'full' : ''}
                value={value}
                required={field.required}
                onChange={event => update(field.name, event.target.value)}
              />
            );
          }

          if (field.type === 'select') {
            return (
              <Select
                key={field.name}
                label={field.label}
                options={field.options}
                className={field.full ? 'full' : ''}
                value={value}
                required={field.required}
                onChange={event => update(field.name, event.target.value)}
              />
            );
          }

          return (
            <Input
              key={field.name}
              label={field.label}
              type={field.type || 'text'}
              className={field.full ? 'full' : ''}
              value={value}
              required={field.required}
              min={field.min}
              onChange={event => update(field.name, event.target.value)}
            />
          );
        })}
      </form>
    </Modal>
  );
}
