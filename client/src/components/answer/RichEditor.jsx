import { useEffect, useRef, useState } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import Placeholder from '@tiptap/extension-placeholder'
import CodeBlockLowlight from '@tiptap/extension-code-block-lowlight'
import { common, createLowlight } from 'lowlight'
import {
  Bold, Italic, Strikethrough, Heading2, Heading3, List, ListOrdered, Quote,
  Code2, Link2, ImagePlus, Undo2, Redo2, Loader2,
} from 'lucide-react'
import { uploadImage } from '../../lib/api'
import { enforceSafeLinks } from '../../lib/rich'

const lowlight = createLowlight(common)

function MenuButton({ onClick, active = false, disabled = false, title, children }) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={title}
      aria-label={title}
      className={`p-1.5 rounded-md transition-colors ${
        active ? 'bg-brand-50 text-brand' : 'text-ink-soft hover:bg-ink/[0.06] hover:text-ink'
      } disabled:opacity-40`}
    >
      {children}
    </button>
  )
}

export default function RichEditor({ value, onChange, placeholder = 'Write what worked… include commands, error text and steps.', minHeight = 130, onError }) {
  const [uploading, setUploading] = useState(false)
  const fileRef = useRef(null)
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange

  const editor = useEditor({
    extensions: [
      StarterKit.configure({ codeBlock: false, heading: { levels: [2, 3] } }),
      CodeBlockLowlight.configure({ lowlight, defaultLanguage: 'plaintext' }),
      Link.configure({ openOnClick: false, autolink: true }),
      Image,
      Placeholder.configure({ placeholder }),
    ],
    content: value || '',
    editorProps: {
      attributes: { class: 'tiptap' },
    },
    onUpdate: ({ editor }) => onChangeRef.current(editor.getHTML()),
  })

  // image drag-drop & paste
  useEffect(() => {
    if (!editor) return
    const dom = editor.view.dom
    const insertImage = async (file) => {
      if (!file.type.startsWith('image/')) return
      setUploading(true)
      try {
        const { url } = await uploadImage(file)
        editor.chain().focus().setImage({ src: url }).run()
      } catch (err) {
        onError?.(err)
      } finally {
        setUploading(false)
      }
    }
    const onDrop = (e) => {
      const file = e.dataTransfer?.files?.[0]
      if (file) {
        e.preventDefault()
        insertImage(file)
      }
    }
    const onPaste = (e) => {
      const file = e.clipboardData?.files?.[0]
      if (file) {
        e.preventDefault()
        insertImage(file)
      }
    }
    dom.addEventListener('drop', onDrop)
    dom.addEventListener('paste', onPaste)
    return () => {
      dom.removeEventListener('drop', onDrop)
      dom.removeEventListener('paste', onPaste)
    }
  }, [editor, onError])

  useEffect(() => {
    if (editor) enforceSafeLinks(editor.view.dom)
  })

  if (!editor) return <div className="skeleton h-32" />

  const pickImage = () => fileRef.current?.click()
  const onFile = async (e) => {
    const file = e.target.files?.[0]
    if (file) {
      setUploading(true)
      try {
        const { url } = await uploadImage(file)
        editor.chain().focus().setImage({ src: url }).run()
      } catch (err) {
        onError?.(err)
      } finally {
        setUploading(false)
        e.target.value = ''
      }
    }
  }

  const setLink = () => {
    const previous = editor.getAttributes('link').href
    const url = window.prompt('Link URL', previous)
    if (url === null) return
    if (url === '') {
      editor.chain().focus().unsetLink().run()
      return
    }
    editor.chain().focus().setLink({ href: url }).run()
  }

  return (
    <div className="tiptap-editor border border-line rounded-lg overflow-hidden focus-within:border-brand/50 focus-within:ring-2 focus-within:ring-brand/15 transition-shadow bg-white">
      <div className="flex items-center flex-wrap gap-0.5 px-2 py-1.5 border-b border-line/80 bg-[#FAFAFB]">
        <MenuButton title="Bold" onClick={() => editor.chain().focus().toggleBold().run()} active={editor.isActive('bold')}>
          <Bold size={16} />
        </MenuButton>
        <MenuButton title="Italic" onClick={() => editor.chain().focus().toggleItalic().run()} active={editor.isActive('italic')}>
          <Italic size={16} />
        </MenuButton>
        <MenuButton title="Strikethrough" onClick={() => editor.chain().focus().toggleStrike().run()} active={editor.isActive('strike')}>
          <Strikethrough size={16} />
        </MenuButton>
        <span className="w-px h-5 bg-line mx-1" />
        <MenuButton title="Heading" onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} active={editor.isActive('heading', { level: 2 })}>
          <Heading2 size={16} />
        </MenuButton>
        <MenuButton title="Subheading" onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} active={editor.isActive('heading', { level: 3 })}>
          <Heading3 size={16} />
        </MenuButton>
        <span className="w-px h-5 bg-line mx-1" />
        <MenuButton title="Bulleted list" onClick={() => editor.chain().focus().toggleBulletList().run()} active={editor.isActive('bulletList')}>
          <List size={16} />
        </MenuButton>
        <MenuButton title="Numbered list" onClick={() => editor.chain().focus().toggleOrderedList().run()} active={editor.isActive('orderedList')}>
          <ListOrdered size={16} />
        </MenuButton>
        <MenuButton title="Quote" onClick={() => editor.chain().focus().toggleBlockquote().run()} active={editor.isActive('blockquote')}>
          <Quote size={16} />
        </MenuButton>
        <MenuButton title="Code block" onClick={() => editor.chain().focus().toggleCodeBlock().run()} active={editor.isActive('codeBlock')}>
          <Code2 size={16} />
        </MenuButton>
        <span className="w-px h-5 bg-line mx-1" />
        <MenuButton title="Link" onClick={setLink} active={editor.isActive('link')}>
          <Link2 size={16} />
        </MenuButton>
        <MenuButton title="Insert image" onClick={pickImage} disabled={uploading}>
          {uploading ? <Loader2 size={16} className="animate-spin" /> : <ImagePlus size={16} />}
        </MenuButton>
        <span className="flex-1" />
        <MenuButton title="Undo" onClick={() => editor.chain().focus().undo().run()} disabled={!editor.can().undo()}>
          <Undo2 size={16} />
        </MenuButton>
        <MenuButton title="Redo" onClick={() => editor.chain().focus().redo().run()} disabled={!editor.can().redo()}>
          <Redo2 size={16} />
        </MenuButton>
      </div>
      <EditorContent editor={editor} />
      <input ref={fileRef} type="file" accept="image/png,image/jpeg,image/gif,image/webp" hidden onChange={onFile} />
    </div>
  )
}
