import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, CheckCircle2, Image as ImageIcon, Receipt, Upload } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import { z } from 'zod'

import Button from '../components/Button.jsx'
import LayoutSAV from '../layouts/LayoutSAV.jsx'
import SelectField from '../components/SelectField.jsx'
import TextAreaField from '../components/TextAreaField.jsx'
import TextField from '../components/TextField.jsx'
import { createReclamation, uploadReclamationFile } from '../services/reclamations.service.js'
import { Priority, priorityLabel } from '../utils/enums.js'

const optionalText = (max) => z.string().max(max).optional().or(z.literal(''))

const schema = z.object({
  description: z.string().min(1, 'La description est obligatoire.').max(500),
  priority: z.coerce.number().int().min(0).max(3),
  productName: optionalText(150),
  barcode: optionalText(64),
  brand: optionalText(100),
  model: optionalText(100),
  serialNumber: optionalText(100),
  productReference: optionalText(100),
  sellerName: optionalText(150),
  purchaseDate: z.string().optional().or(z.literal('')),
})

export default function ReclamationCreatePage() {
  const navigate = useNavigate()
  const [imageFile, setImageFile] = useState(null)
  const [imagePreview, setImagePreview] = useState('')
  const [proofFile, setProofFile] = useState(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      description: '',
      priority: Priority.MEDUIM,
      productName: '',
      barcode: '',
      brand: '',
      model: '',
      serialNumber: '',
      productReference: '',
      sellerName: '',
      purchaseDate: '',
    },
  })

  const imageHint = useMemo(() => {
    if (!imageFile) return 'PNG, JPG ou WEBP, maximum 5 MB.'
    return `${imageFile.name} (${Math.round(imageFile.size / 1024)} KB)`
  }, [imageFile])

  const proofHint = useMemo(() => {
    if (!proofFile) return 'PDF ou image, maximum 10 MB.'
    return `${proofFile.name} (${Math.round(proofFile.size / 1024)} KB)`
  }, [proofFile])

  function validateImage(file) {
    if (!file) return { ok: true }
    if (!file.type.startsWith('image/')) return { ok: false, message: 'Le fichier image doit etre au format image.' }
    if (file.size > 5 * 1024 * 1024) return { ok: false, message: 'L image doit etre inferieure ou egale a 5 MB.' }
    return { ok: true }
  }

  function validateProof(file) {
    if (!file) return { ok: true }
    const isImage = file.type.startsWith('image/')
    const isPdf = file.type === 'application/pdf'
    if (!isImage && !isPdf) return { ok: false, message: 'La preuve d achat doit etre un PDF ou une image.' }
    if (file.size > 10 * 1024 * 1024) return { ok: false, message: 'La preuve d achat doit etre inferieure ou egale a 10 MB.' }
    return { ok: true }
  }

  async function onSubmit(values) {
    try {
      const imageCheck = validateImage(imageFile)
      if (!imageCheck.ok) {
        setError('root', { message: imageCheck.message })
        return
      }

      const proofCheck = validateProof(proofFile)
      if (!proofCheck.ok) {
        setError('root', { message: proofCheck.message })
        return
      }

      let productImageUrl
      let purchaseProofUrl

      if (imageFile) {
        const upload = await uploadReclamationFile(imageFile, 'image')
        productImageUrl = upload?.url
      }

      if (proofFile) {
        const upload = await uploadReclamationFile(proofFile, 'proof')
        purchaseProofUrl = upload?.url
      }

      const created = await createReclamation({
        description: values.description.trim(),
        priority: values.priority,
        productName: values.productName?.trim() || undefined,
        barcode: values.barcode?.trim() || undefined,
        brand: values.brand?.trim() || undefined,
        model: values.model?.trim() || undefined,
        serialNumber: values.serialNumber?.trim() || undefined,
        productReference: values.productReference?.trim() || undefined,
        sellerName: values.sellerName?.trim() || undefined,
        purchaseDate: values.purchaseDate || undefined,
        productImageUrl,
        purchaseProofUrl,
      })

      toast.success('Reclamation creee.')
      navigate(`/app/reclamations/${created.id}`, { replace: true })
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError('root', { message: message || 'Creation impossible.' })
    }
  }

  return (
    <LayoutSAV
      title="Ajouter une reclamation"
      description="Le formulaire met en avant les champs obligatoires, les pieces jointes utiles et des messages d erreur clairs."
      meta={<>Le ticket sera cree puis ouvert directement en detail.</>}
      actions={
        <Link to="/app/reclamations">
          <Button variant="secondary">
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            Retour
          </Button>
        </Link>
      }
    >
      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_320px]">
        <form className="surface-solid space-y-6 p-6" onSubmit={handleSubmit(onSubmit)}>
          <div>
            <h2 className="text-lg font-bold text-slate-950">Informations obligatoires</h2>
            <p className="mt-1 text-sm text-slate-600">Commencez par decrire le probleme et son niveau de priorite.</p>
          </div>

          <TextAreaField
            label="Description"
            placeholder="Decrivez le probleme, les symptomes observes et l urgence du ticket..."
            error={errors.description?.message}
            rows={6}
            required
            {...register('description')}
          />

          <SelectField label="Priorite" error={errors.priority?.message} required {...register('priority')}>
            <option value={Priority.LOW}>{priorityLabel(Priority.LOW)}</option>
            <option value={Priority.MEDUIM}>{priorityLabel(Priority.MEDUIM)}</option>
            <option value={Priority.HIGH}>{priorityLabel(Priority.HIGH)}</option>
            <option value={Priority.URGENT}>{priorityLabel(Priority.URGENT)}</option>
          </SelectField>

          <div className="rounded-[24px] border border-slate-200 bg-slate-50 p-5">
            <h3 className="text-lg font-bold text-slate-950">Informations produit</h3>
            <p className="mt-1 text-sm text-slate-600">Ces champs sont optionnels mais utiles pour qualifier plus vite le ticket SAV.</p>

            <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
              <TextField label="Nom du produit" placeholder="Ex: Chauffe-eau" {...register('productName')} />
              <TextField label="Code-barres" placeholder="Ex: 3024990123456" {...register('barcode')} />
              <TextField label="Marque" placeholder="Ex: Ariston" {...register('brand')} />
              <TextField label="Modele" placeholder="Ex: PRO1 ECO" {...register('model')} />
              <TextField label="Numero de serie" placeholder="Ex: SN123456" {...register('serialNumber')} />
              <TextField label="Reference produit" placeholder="Ex: REF-2024-01" {...register('productReference')} />
              <TextField label="Vendeur / magasin" placeholder="Ex: Carrefour" {...register('sellerName')} />
              <TextField label="Date d'achat" type="date" {...register('purchaseDate')} />
            </div>
          </div>

          {errors.root?.message ? <div className="notice-error">{errors.root.message}</div> : null}

          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
            <Button type="submit" size="lg" disabled={isSubmitting}>
              <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
              {isSubmitting ? 'Creation...' : 'Ajouter la reclamation'}
            </Button>
          </div>
        </form>

        <aside className="space-y-6">
          <div className="surface-solid p-6">
            <div className="flex items-center gap-3">
              <div className="grid h-12 w-12 place-items-center rounded-2xl bg-cyan-50 text-cyan-700">
                <ImageIcon className="h-5 w-5" aria-hidden="true" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-slate-950">Image du produit</h3>
                <p className="mt-1 text-sm text-slate-600">Facultatif mais utile pour accelerer l analyse.</p>
              </div>
            </div>

            <label className="mt-5 block">
              <div className="input-control-soft">
                <input
                  type="file"
                  accept="image/*"
                  onChange={(event) => {
                    const file = event.target.files?.[0] || null
                    setImageFile(file)
                    if (file) {
                      const reader = new FileReader()
                      reader.onload = () => setImagePreview(String(reader.result || ''))
                      reader.readAsDataURL(file)
                    } else {
                      setImagePreview('')
                    }
                  }}
                />
                <div className="mt-3 text-xs text-slate-500">{imageHint}</div>
              </div>
            </label>

            {imagePreview ? (
              <div className="mt-4 overflow-hidden rounded-[22px] border border-slate-200 bg-slate-100">
                <img src={imagePreview} alt="Apercu du produit" className="h-56 w-full object-cover" />
              </div>
            ) : (
              <div className="mt-4 rounded-[22px] border border-dashed border-slate-200 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">
                Aucune image selectionnee.
              </div>
            )}
          </div>

          <div className="surface-solid p-6">
            <div className="flex items-center gap-3">
              <div className="grid h-12 w-12 place-items-center rounded-2xl bg-amber-50 text-amber-700">
                <Receipt className="h-5 w-5" aria-hidden="true" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-slate-950">Preuve d'achat</h3>
                <p className="mt-1 text-sm text-slate-600">PDF ou image pour documenter le dossier.</p>
              </div>
            </div>

            <label className="mt-5 block">
              <div className="input-control-soft">
                <input
                  type="file"
                  accept="image/*,application/pdf"
                  onChange={(event) => setProofFile(event.target.files?.[0] || null)}
                />
                <div className="mt-3 text-xs text-slate-500">{proofHint}</div>
                <div className="mt-3 inline-flex items-center gap-2 text-xs font-semibold text-slate-600">
                  <Upload className="h-4 w-4" aria-hidden="true" />
                  PDF ou image, maximum 10 MB.
                </div>
              </div>
            </label>
          </div>
        </aside>
      </div>
    </LayoutSAV>
  )
}
