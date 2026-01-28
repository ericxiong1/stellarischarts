import React, { useEffect, useMemo, useState } from 'react';
import { api, type SpeciesBreakdown } from '../api';
import { Card, CardContent } from '@/components/ui/card';
import { Zap, Gem, Apple, TrendingUp, ChevronUp, ChevronDown } from 'lucide-react';
import { ResponsiveContainer, PieChart, Pie, Cell, Tooltip, Legend } from 'recharts';

export const GalacticScreen: React.FC = () => {
  const [totals, setTotals] = useState<Record<string, number>>({});
  const [previousTotals, setPreviousTotals] = useState<Record<string, number>>({});
  const [incomeTotals, setIncomeTotals] = useState<Record<string, number>>({});
  const [expenseTotals, setExpenseTotals] = useState<Record<string, number>>({});
  const [previousIncomeTotals, setPreviousIncomeTotals] = useState<Record<string, number>>({});
  const [previousExpenseTotals, setPreviousExpenseTotals] = useState<Record<string, number>>({});
  const [speciesBreakdown, setSpeciesBreakdown] = useState<SpeciesBreakdown[]>([]);
  const [previousSpeciesBreakdown, setPreviousSpeciesBreakdown] = useState<SpeciesBreakdown[]>([]);
  const [gameDate, setGameDate] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchSummary = async () => {
      try {
        setLoading(true);
        const data = await api.getGalaxySummary();
        setTotals(data.totals ?? {});
        setPreviousTotals(data.previousTotals ?? {});
        setIncomeTotals(data.incomeTotals ?? {});
        setExpenseTotals(data.expenseTotals ?? {});
        setPreviousIncomeTotals(data.previousIncomeTotals ?? {});
        setPreviousExpenseTotals(data.previousExpenseTotals ?? {});
        setGameDate(data.gameDate ?? null);
        const species = await api.getGalaxySpecies();
        setSpeciesBreakdown(species);
        const prevSpecies = await api.getGalaxySpeciesPrevious();
        setPreviousSpeciesBreakdown(prevSpecies);
      } finally {
        setLoading(false);
      }
    };

    fetchSummary();
  }, []);

  const speciesChartData = useMemo(
    () => groupSpeciesData(speciesBreakdown, 2),
    [speciesBreakdown]
  );

  const previousSpeciesChartData = useMemo(
    () => groupSpeciesData(previousSpeciesBreakdown, 2),
    [previousSpeciesBreakdown]
  );

  const speciesDeltaByName = useMemo(() => {
    const prevMap = new Map(previousSpeciesChartData.map((item) => [item.speciesName, item.amount]));
    const current = new Map(speciesChartData.map((item) => [item.speciesName, item.amount]));
    const allNames = new Set([...prevMap.keys(), ...current.keys()]);
    const result = new Map<string, number>();
    allNames.forEach((name) => {
      const prev = prevMap.get(name) ?? 0;
      const curr = current.get(name) ?? 0;
      result.set(name, curr - prev);
    });
    return result;
  }, [previousSpeciesChartData, speciesChartData]);

  const speciesColors = [
    '#3b82f6',
    '#22c55e',
    '#f59e0b',
    '#ef4444',
    '#a855f7',
    '#ec4899',
    '#14b8a6',
    '#eab308',
    '#0ea5e9',
    '#f97316',
    '#8b5cf6',
    '#84cc16',
  ];

  return (
    <div className="flex-1 overflow-y-auto bg-background p-8 text-foreground">
      <h2 className="mb-4 text-2xl font-semibold">Galactic Screen</h2>
      <p className="mb-6 text-sm text-muted-foreground">
        Galaxywide monthly net resources{gameDate ? ` - ${gameDate}` : ''}
      </p>

      {loading ? (
        <p className="text-muted-foreground">Loading summary...</p>
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {[
            { key: 'energy', label: 'Energy Credits', Icon: Zap, iconClass: 'text-yellow-300' },
            { key: 'minerals', label: 'Minerals', Icon: Gem, iconClass: 'text-red-400' },
            { key: 'food', label: 'Food', Icon: Apple, iconClass: 'text-emerald-300' },
            { key: 'trade_value', label: 'Trade', Icon: TrendingUp, iconClass: 'text-white' },
          ].map((resource) => {
            const value =
              resource.key === 'trade_value'
                ? totals.trade_value ?? totals.trade ?? 0
                : totals[resource.key] ?? 0;
            const incomeValue =
              resource.key === 'trade_value'
                ? incomeTotals.trade_value ?? incomeTotals.trade ?? 0
                : incomeTotals[resource.key] ?? 0;
            const expenseValue =
              resource.key === 'trade_value'
                ? expenseTotals.trade_value ?? expenseTotals.trade ?? 0
                : expenseTotals[resource.key] ?? 0;
            const prevIncomeValue =
              resource.key === 'trade_value'
                ? previousIncomeTotals.trade_value ?? previousIncomeTotals.trade ?? 0
                : previousIncomeTotals[resource.key] ?? 0;
            const prevExpenseValue =
              resource.key === 'trade_value'
                ? previousExpenseTotals.trade_value ?? previousExpenseTotals.trade ?? 0
                : previousExpenseTotals[resource.key] ?? 0;
            const prevValue =
              resource.key === 'trade_value'
                ? previousTotals.trade_value ?? previousTotals.trade ?? 0
                : previousTotals[resource.key] ?? 0;
            const deltaPct =
              prevValue === 0 ? null : ((value - prevValue) / Math.abs(prevValue)) * 100;
            const deltaValue = value - prevValue;
            const deltaClass = deltaValue >= 0 ? 'text-emerald-300' : 'text-red-400';
            const valueClass = value >= 0 ? 'text-emerald-400' : 'text-red-400';
            const DeltaIcon = deltaValue >= 0 ? ChevronUp : ChevronDown;
            const expenseNetValue = Math.abs(expenseValue);
            const prevExpenseNetValue = Math.abs(prevExpenseValue);
            const incomeDeltaValue = incomeValue - prevIncomeValue;
            const expenseDeltaValue = expenseNetValue - prevExpenseNetValue;
            const incomeDeltaPct =
              prevIncomeValue === 0 ? null : (incomeDeltaValue / Math.abs(prevIncomeValue)) * 100;
            const expenseDeltaPct =
              prevExpenseNetValue === 0
                ? null
                : (expenseDeltaValue / Math.abs(prevExpenseNetValue)) * 100;
            const incomeDeltaClass = incomeDeltaValue >= 0 ? 'text-emerald-300' : 'text-red-400';
            const expenseDeltaClass = expenseDeltaValue >= 0 ? 'text-red-400' : 'text-emerald-300';
            return (
              <Card key={resource.key} className="border-border bg-gradient-to-b from-[#151515] to-[#0f0f0f] shadow-md">
                <CardContent className="space-y-4 p-4">
                  <div className="flex items-center justify-between">
                    <div className="rounded-lg bg-black/40 p-2">
                      <resource.Icon className={`h-4 w-4 ${resource.iconClass}`} />
                    </div>
                    {deltaPct === null ? null : (
                      <div className={`flex items-center gap-1 text-xs font-semibold ${deltaClass}`}>
                        <span>
                          {formatDelta(deltaPct)}
                          {` (${formatSigned(deltaValue)})`}
                        </span>
                        <DeltaIcon className="h-3.5 w-3.5" />
                      </div>
                    )}
                  </div>
                  <div>
                    <div className={`text-2xl font-semibold ${valueClass}`}>{formatSigned(value)}</div>
                    <div className="text-xs text-muted-foreground">{resource.label}</div>
                  </div>
                    <div className="space-y-1 text-xs text-muted-foreground">
                    <div className="flex items-center justify-between">
                      <span>Gross Income</span>
                      <span className="font-semibold text-emerald-300">
                        {formatCompact(incomeValue)}
                        {incomeDeltaPct === null ? '' : (
                          <span className={`ml-1 ${incomeDeltaClass}`}>
                            ({formatSigned(incomeDeltaValue)}, {formatDelta(incomeDeltaPct)})
                          </span>
                        )}
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span>Net Expenses</span>
                      <span className="font-semibold text-red-400">
                        {formatCompact(expenseNetValue)}
                        {expenseDeltaPct === null ? '' : (
                          <span className={`ml-1 ${expenseDeltaClass}`}>
                            ({formatSigned(expenseDeltaValue)}, {formatDelta(expenseDeltaPct)})
                          </span>
                        )}
                      </span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
          </div>
          <h2 className="mb-3 mt-6 text-xl font-semibold">Population Demographics</h2>
          <Card className="border-border">
            <CardContent className="p-4">
              {speciesChartData.length === 0 ? (
                <p className="text-sm text-muted-foreground">No species data available</p>
              ) : (
                <div className="h-72">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={speciesChartData}
                        dataKey="amount"
                        nameKey="speciesName"
                        cx="50%"
                        cy="50%"
                        outerRadius={100}
                        label={false}
                        labelLine={false}
                      >
                        {speciesChartData.map((entry, index) => (
                          <Cell
                            key={`${entry.speciesName}-${index}`}
                            fill={speciesColors[index % speciesColors.length]}
                          />
                        ))}
                      </Pie>
                      <Tooltip
                        content={
                          <SpeciesTooltip
                            total={sumSpeciesAmount(speciesChartData)}
                            deltaByName={speciesDeltaByName}
                          />
                        }
                      />
                      <Legend content={<SpeciesLegend data={speciesChartData} colors={speciesColors} />} />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
};

function formatCompact(value: number): string {
  const abs = Math.abs(value);
  if (abs >= 1_000_000) return `${(value / 1_000_000).toFixed(2)}M`;
  if (abs >= 1_000) return `${(value / 1_000).toFixed(2)}K`;
  return value.toFixed(2);
}

function formatSigned(value: number): string {
  const sign = value >= 0 ? '+' : '';
  return `${sign}${formatCompact(value)}`;
}

function formatDelta(value: number): string {
  const sign = value >= 0 ? '+' : '';
  return `${sign}${value.toFixed(0)}%`;
}

function groupSpeciesData(data: SpeciesBreakdown[], minPercent: number): SpeciesBreakdown[] {
  if (data.length === 0) return [];
  const sorted = [...data].sort((a, b) => b.amount - a.amount);
  const total = sorted.reduce((sum, item) => sum + item.amount, 0);
  if (total <= 0) return sorted;

  const kept: SpeciesBreakdown[] = [];
  let otherTotal = 0;
  for (const item of sorted) {
    const pct = (item.amount / total) * 100;
    if (pct < minPercent) {
      otherTotal += item.amount;
    } else {
      kept.push(item);
    }
  }

  if (otherTotal > 0) {
    kept.push({ speciesName: 'Other', amount: otherTotal });
  }

  const withoutOther = kept.filter((item) => item.speciesName !== 'Other');
  const otherItem = kept.find((item) => item.speciesName === 'Other');
  return otherItem ? [...withoutOther, otherItem] : withoutOther;
}

function sumSpeciesAmount(data: SpeciesBreakdown[]): number {
  return data.reduce((sum, item) => sum + item.amount, 0);
}

function formatWhole(value: number): string {
  return Math.round(value).toLocaleString();
}

function formatSignedWhole(value: number): string {
  const sign = value >= 0 ? '+' : '';
  return `${sign}${formatWhole(value)}`;
}

function SpeciesTooltip({
  active,
  payload,
  total,
  deltaByName,
}: {
  active?: boolean;
  payload?: any[];
  total: number;
  deltaByName?: Map<string, number>;
}) {
  if (!active || !payload?.length) return null;
  const entry = payload[0];
  const value = Number(entry.value ?? 0);
  const percent = total > 0 ? (value / total) * 100 : 0;
  const delta = deltaByName?.get(entry.name ?? '') ?? 0;
  const deltaText = delta === 0 ? '+0' : formatSignedWhole(delta);
  const deltaColor = delta >= 0 ? 'rgb(52 211 153)' : 'rgb(248 113 113)';
  return (
    <div
      style={{
        background: 'oklch(var(--background))',
        border: '1px solid oklch(var(--border))',
        borderRadius: '8px',
        padding: '8px 10px',
        color: 'oklch(var(--foreground))',
      }}
    >
      <div style={{ fontWeight: 600, marginBottom: 4 }}>{entry.name ?? 'Species'}</div>
      <div style={{ fontSize: 12, opacity: 0.9 }}>
        {formatWhole(value)} ({percent.toFixed(1)}%)
      </div>
      <div style={{ fontSize: 12, marginTop: 4, color: deltaColor }}>
        Δ {deltaText}
      </div>
    </div>
  );
}

function SpeciesLegend({ data, colors }: { data: SpeciesBreakdown[]; colors: string[] }) {
  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px 16px', fontSize: 14, marginTop: 10 }}>
      {data.map((item, index) => (
        <div key={`${item.speciesName}-${index}`} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span
            style={{
              width: 12,
              height: 12,
              borderRadius: 999,
              background: colors[index % colors.length],
              display: 'inline-block',
            }}
          />
          <span>{item.speciesName}</span>
        </div>
      ))}
    </div>
  );
}




