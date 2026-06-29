import { useRef, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import {
  Upload, FileSpreadsheet, ArrowDownLeft, ArrowUpRight, Scale, AlertCircle, Landmark, Save, CheckCircle2,
} from 'lucide-react'
import { bankApi } from '@/api/bank'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import SummaryCard from '@/components/common/SummaryCard'
import { formatCurrency, formatDate } from '@/lib/utils'
import { useToast } from '@/hooks/useToast'
import type { BankStatementPreview } from '@/types'

export default function BankImport() {
  const inputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()
  const { toast } = useToast()
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<BankStatementPreview | null>(null)

  const mutation = useMutation({
    mutationFn: (f: File) => bankApi.previewStatement(f),
    onSuccess: (data) => setPreview(data),
  })

  const importMut = useMutation({
    mutationFn: (f: File) => bankApi.importStatement(f),
    onSuccess: (res) => {
      toast({ title: `Imported ${res.imported} transactions`, description: res.skippedDuplicates ? `${res.skippedDuplicates} duplicates skipped` : undefined })
    },
  })

  const onPick = (f: File | null) => {
    setFile(f)
    setPreview(null)
    if (f) mutation.mutate(f)
  }

  return (
    <div className="space-y-6">
      {/* Upload card */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Landmark className="h-5 w-5 text-primary" /> Import Bank Statement
          </CardTitle>
          <CardDescription>
            Upload your HDFC account statement (.xls / .xlsx). This is a preview — nothing is saved yet.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div
            className="flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-8 text-center cursor-pointer hover:bg-muted/30 transition-colors"
            onClick={() => inputRef.current?.click()}
            onDragOver={(e) => e.preventDefault()}
            onDrop={(e) => {
              e.preventDefault()
              onPick(e.dataTransfer.files?.[0] ?? null)
            }}
          >
            <FileSpreadsheet className="h-10 w-10 text-muted-foreground mb-3" />
            <p className="text-sm font-medium">Click to choose, or drag &amp; drop your statement file</p>
            <p className="text-xs text-muted-foreground mt-1">Accepted: .xls, .xlsx (HDFC account statement)</p>
            {file && <Badge variant="secondary" className="mt-3">{file.name}</Badge>}
            <input
              ref={inputRef}
              type="file"
              accept=".xls,.xlsx,application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              className="hidden"
              onChange={(e) => onPick(e.target.files?.[0] ?? null)}
            />
          </div>

          {mutation.isPending && (
            <p className="text-sm text-muted-foreground mt-4 flex items-center gap-2">
              <Upload className="h-4 w-4 animate-pulse" /> Parsing statement…
            </p>
          )}
          {mutation.isError && (
            <div className="mt-4 flex items-center gap-2 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
              <AlertCircle className="h-4 w-4 shrink-0" />
              {mutation.error instanceof Error ? mutation.error.message : 'Failed to parse the file.'}
            </div>
          )}
        </CardContent>
      </Card>

      {preview && (
        <>
          {/* Account + summary */}
          <Card>
            <CardContent className="p-4 flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="font-semibold">{preview.accountName ?? 'Account'}</p>
                <p className="text-xs text-muted-foreground">
                  {preview.bank} · A/C {preview.accountNumber} · {preview.period}
                </p>
              </div>
              <div className="flex items-center gap-3">
                <Badge variant="outline">{preview.transactionCount} transactions</Badge>
                {importMut.isSuccess ? (
                  <Button size="sm" variant="outline" onClick={() => navigate('/bank/dashboard')}>
                    <CheckCircle2 className="h-4 w-4" /> View Dashboard
                  </Button>
                ) : (
                  <Button size="sm" onClick={() => file && importMut.mutate(file)} disabled={importMut.isPending}>
                    <Save className="h-4 w-4" />
                    {importMut.isPending ? 'Importing…' : 'Import & Save'}
                  </Button>
                )}
              </div>
            </CardContent>
          </Card>
          {importMut.isError && (
            <div className="flex items-center gap-2 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
              <AlertCircle className="h-4 w-4 shrink-0" />
              {importMut.error instanceof Error ? importMut.error.message : 'Import failed.'}
            </div>
          )}
          {importMut.isSuccess && (
            <div className="flex items-center gap-2 rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-800">
              <CheckCircle2 className="h-4 w-4 shrink-0" />
              Saved {importMut.data.imported} transactions{importMut.data.skippedDuplicates > 0 ? `, skipped ${importMut.data.skippedDuplicates} duplicates` : ''}. Open the Bank Dashboard to see the analysis.
            </div>
          )}

          <div className="grid gap-4 grid-cols-1 sm:grid-cols-3">
            <SummaryCard title="Money In (Credits)" value={preview.totalDeposit} icon={ArrowDownLeft} iconColor="text-emerald-600" />
            <SummaryCard title="Money Out (Debits)" value={preview.totalWithdrawal} icon={ArrowUpRight} iconColor="text-red-600" />
            <SummaryCard
              title="Net Change"
              value={preview.netChange}
              icon={Scale}
              iconColor={preview.netChange >= 0 ? 'text-emerald-600' : 'text-red-600'}
              description={preview.netChange >= 0 ? 'Net inflow' : 'Net outflow'}
            />
          </div>

          {/* Transactions table */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Parsed Transactions</CardTitle>
              <CardDescription>Review the data below. Once it looks right, we'll add saving, categories, and dashboards.</CardDescription>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto max-h-[60vh]">
                <table className="w-full text-sm">
                  <thead className="sticky top-0 bg-muted">
                    <tr className="border-b">
                      <th className="text-left px-3 py-2.5 font-medium text-muted-foreground">Date</th>
                      <th className="text-left px-3 py-2.5 font-medium text-muted-foreground">Narration</th>
                      <th className="text-left px-3 py-2.5 font-medium text-muted-foreground">Type</th>
                      <th className="text-left px-3 py-2.5 font-medium text-muted-foreground">Category</th>
                      <th className="text-right px-3 py-2.5 font-medium text-muted-foreground">Out</th>
                      <th className="text-right px-3 py-2.5 font-medium text-muted-foreground">In</th>
                      <th className="text-right px-3 py-2.5 font-medium text-muted-foreground">Balance</th>
                    </tr>
                  </thead>
                  <tbody>
                    {preview.transactions.map((t) => (
                      <tr key={t.rowNumber} className="border-b hover:bg-muted/20">
                        <td className="px-3 py-2 whitespace-nowrap">{formatDate(t.date)}</td>
                        <td className="px-3 py-2 max-w-[280px] truncate" title={t.narration}>{t.narration}</td>
                        <td className="px-3 py-2">
                          <Badge variant="outline" className="text-[10px] px-1.5 py-0">{t.guessedType}</Badge>
                        </td>
                        <td className="px-3 py-2 text-muted-foreground text-xs">{t.guessedCategory}</td>
                        <td className="px-3 py-2 text-right text-red-600">{t.withdrawal > 0 ? formatCurrency(t.withdrawal) : '—'}</td>
                        <td className="px-3 py-2 text-right text-emerald-600">{t.deposit > 0 ? formatCurrency(t.deposit) : '—'}</td>
                        <td className="px-3 py-2 text-right text-muted-foreground">
                          {t.closingBalance != null ? formatCurrency(t.closingBalance) : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}
