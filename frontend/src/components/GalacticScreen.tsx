import React, { useEffect, useMemo, useState } from 'react';
import { api, type SpeciesBreakdown, type GalaxySpeciesHistory } from '../api';
import { Card, CardContent } from '@/components/ui/card';
import {
  Zap,
  Gem,
  Apple,
  TrendingUp,
  Hammer,
  Boxes,
  Sparkles,
  FlaskConical,
  Flame,
  Wind,
  Moon,
  Magnet,
  Droplets,
  Cpu,
  Crown,
  ChevronUp,
  ChevronDown,
} from 'lucide-react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  LineChart,
  Line,
  CartesianGrid,
  XAxis,
  YAxis,
} from 'recharts';

export const GalacticScreen: React.FC = () => {
  const [totals, setTotals] = useState<Record<string, number>>({});
  const [previousTotals, setPreviousTotals] = useState<Record<string, number>>({});
  const [incomeTotals, setIncomeTotals] = useState<Record<string, number>>({});
  const [expenseTotals, setExpenseTotals] = useState<Record<string, number>>({});
  const [previousIncomeTotals, setPreviousIncomeTotals] = useState<Record<string, number>>({});
  const [previousExpenseTotals, setPreviousExpenseTotals] = useState<Record<string, number>>({});
  const [speciesBreakdown, setSpeciesBreakdown] = useState<SpeciesBreakdown[]>([]);
  const [previousSpeciesBreakdown, setPreviousSpeciesBreakdown] = useState<SpeciesBreakdown[]>([]);
  const [speciesHistory, setSpeciesHistory] = useState<GalaxySpeciesHistory[]>([]);
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
        const history = await api.getGalaxySpeciesHistory();
        setSpeciesHistory(history);
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

  const speciesTrendData = useMemo(() => {
    if (speciesHistory.length === 0) return [];
    const trendSpecies = speciesChartData.map((item) => item.speciesName);
    const topSpecies = trendSpecies.filter((name) => name !== 'Other');

    const grouped = new Map<string, GalaxySpeciesHistory[]>();
    for (const entry of speciesHistory) {
      if (!grouped.has(entry.gameDate)) {
        grouped.set(entry.gameDate, []);
      }
      grouped.get(entry.gameDate)!.push(entry);
    }

    const orderedDates = [...grouped.keys()].sort((a, b) => parseGameDate(a) - parseGameDate(b));
    return orderedDates.map((gameDate) => {
      const entries = grouped.get(gameDate) ?? [];
      const map = new Map(entries.map((s) => [s.speciesName, s.amount]));
      const total = entries.reduce((sum, s) => sum + s.amount, 0);
      const row: Record<string, number | string> = { gameDate };
      for (const name of topSpecies) {
        row[name] = Number(map.get(name) ?? 0);
      }
      if (trendSpecies.includes('Other')) {
        const knownTotal = topSpecies.reduce((sum, name) => sum + Number(map.get(name) ?? 0), 0);
        row.Other = Math.max(0, total - knownTotal);
      }
      return row;
    });
  }, [speciesHistory, speciesChartData]);

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
          {(() => {
            const resourceCards = [
              { key: 'energy', label: 'Energy Credits', Icon: Zap, iconClass: 'text-yellow-300' },
              { key: 'minerals', label: 'Minerals', Icon: Gem, iconClass: 'text-red-400' },
              { key: 'food', label: 'Food', Icon: Apple, iconClass: 'text-emerald-300' },
              { key: 'trade_value', label: 'Trade', Icon: TrendingUp, iconClass: 'text-white' },
              { key: 'alloys', label: 'Alloys', Icon: Hammer, iconClass: 'text-orange-300' },
              { key: 'consumer_goods', label: 'Consumer Goods', Icon: Boxes, iconClass: 'text-cyan-300' },
              { key: 'unity', label: 'Unity', Icon: Sparkles, iconClass: 'text-fuchsia-300' },
              { key: 'research', label: 'Research', Icon: FlaskConical, iconClass: 'text-sky-300' },
              { key: 'volatile_motes', label: 'Volatile Motes', Icon: Flame, iconClass: 'text-orange-300' },
              { key: 'rare_crystals', label: 'Rare Crystals', Icon: Gem, iconClass: 'text-pink-300' },
              { key: 'exotic_gases', label: 'Exotic Gases', Icon: Wind, iconClass: 'text-cyan-300' },
              { key: 'influence', label: 'Influence', Icon: Crown, iconClass: 'text-yellow-200' },
              { key: 'dark_matter', label: 'Dark Matter', Icon: Moon, iconClass: 'text-indigo-300' },
              { key: 'living_metal', label: 'Living Metal', Icon: Magnet, iconClass: 'text-emerald-300' },
              { key: 'zro', label: 'Zro', Icon: Droplets, iconClass: 'text-violet-300' },
              { key: 'minor_artifacts', label: 'Minor Artifacts', Icon: Sparkles, iconClass: 'text-amber-300' },
              { key: 'astral_threads', label: 'Astral Threads', Icon: Wind, iconClass: 'text-teal-300' },
              { key: 'nanites', label: 'Nanites', Icon: Cpu, iconClass: 'text-slate-200' },
            ].filter((resource) => shouldShowResource(resource.key, incomeTotals, expenseTotals));

            if (resourceCards.length === 0) return null;

            return (
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                {resourceCards.map((resource) => {
                  const value = resolveResource(totals, resource.key);
                  const incomeValue = resolveResource(incomeTotals, resource.key);
                  const expenseValue = resolveResource(expenseTotals, resource.key);
                  const prevIncomeValue = resolveResource(previousIncomeTotals, resource.key);
                  const prevExpenseValue = resolveResource(previousExpenseTotals, resource.key);
                  const prevValue = resolveResource(previousTotals, resource.key);
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
                    prevIncomeValue === 0
                      ? null
                      : (incomeDeltaValue / Math.abs(prevIncomeValue)) * 100;
                  const expenseDeltaPct =
                    prevExpenseNetValue === 0
                      ? null
                      : (expenseDeltaValue / Math.abs(prevExpenseNetValue)) * 100;
                  const incomeDeltaClass = incomeDeltaValue >= 0 ? 'text-emerald-300' : 'text-red-400';
                  const expenseDeltaClass = expenseDeltaValue >= 0 ? 'text-red-400' : 'text-emerald-300';
                  const researchIncome =
                    resource.key === 'research' ? getResearchBreakdown(incomeTotals) : null;
                  const researchPrevIncome =
                    resource.key === 'research' ? getResearchBreakdown(previousIncomeTotals) : null;
                  return (
                    <Card
                      key={resource.key}
                      className="border-border bg-gradient-to-b from-[#151515] to-[#0f0f0f] shadow-md"
                    >
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
                          {resource.key === 'research' && researchIncome && researchPrevIncome ? (
                            <>
                              {[
                                { key: 'physics', label: 'Physics' },
                                { key: 'society', label: 'Society' },
                                { key: 'engineering', label: 'Engineering' },
                              ].map((line) => {
                                const current =
                                  researchIncome[line.key as 'physics' | 'society' | 'engineering'];
                                const previous =
                                  researchPrevIncome[line.key as 'physics' | 'society' | 'engineering'];
                                const deltaValue = current - previous;
                                const deltaPct =
                                  previous === 0 ? null : (deltaValue / Math.abs(previous)) * 100;
                                const deltaClass = deltaValue >= 0 ? 'text-emerald-300' : 'text-red-400';
                                return (
                                  <div key={line.key} className="flex items-center justify-between">
                                    <span>{line.label}</span>
                                    <span className="font-semibold text-emerald-300">
                                      {formatCompact(current)}
                                      {deltaPct === null ? '' : (
                                        <span className={`ml-1 ${deltaClass}`}>
                                          ({formatSigned(deltaValue)}, {formatDelta(deltaPct)})
                                        </span>
                                      )}
                                    </span>
                                  </div>
                                );
                              })}
                            </>
                          ) : (
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
                          )}
                          {resource.key === 'research' ? null : (
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
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            );
          })()}
          <h2 className="mb-3 mt-6 text-xl font-semibold">Population Demographics</h2>
          <div className="grid gap-4 lg:grid-cols-2">
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
            <Card className="border-border">
              <CardContent className="p-4">
                {speciesTrendData.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No species history available</p>
                ) : (
                  <div className="h-72">
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={speciesTrendData}>
                        <CartesianGrid strokeDasharray="3 3" stroke="oklch(var(--border))" />
                        <XAxis
                          dataKey="gameDate"
                          type="category"
                          tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }}
                        />
                        <YAxis
                          tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }}
                          tickFormatter={(value) => formatCompact(Number(value))}
                          allowDecimals={false}
                        />
                        <Tooltip
                          contentStyle={{
                            background: 'oklch(var(--card))',
                            border: '1px solid oklch(var(--border))',
                          }}
                          labelStyle={{ color: 'oklch(var(--muted-foreground))' }}
                          labelFormatter={(_, payload) => payload?.[0]?.payload?.gameDate ?? ''}
                        />
                        <Legend />
                        {speciesChartData.map((species, index) => (
                          <Line
                            key={species.speciesName}
                            type="monotone"
                            dataKey={species.speciesName}
                            stroke={speciesColors[index % speciesColors.length]}
                            dot={false}
                          />
                        ))}
                      </LineChart>
                    </ResponsiveContainer>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
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

function parseGameDate(value: string): number {
  const match = /^(\d{4})\.(\d{2})\.(\d{2})$/.exec(value);
  if (!match) {
    return new Date(value).getTime();
  }
  const year = Number(match[1]);
  const month = Number(match[2]) - 1;
  const day = Number(match[3]);
  return Date.UTC(year, month, day);
}

function getResearchBreakdown(source: Record<string, number>): {
  physics: number;
  society: number;
  engineering: number;
} {
  return {
    physics: source.physics_research ?? source.physics ?? 0,
    society: source.society_research ?? source.society ?? 0,
    engineering: source.engineering_research ?? source.engineering ?? 0,
  };
}

function resolveResource(source: Record<string, number>, key: string): number {
  if (!source) return 0;
  if (key === 'trade_value') {
    return source.trade_value ?? source.trade ?? 0;
  }
  if (key === 'research') {
    let total = 0;
    for (const [resourceKey, value] of Object.entries(source)) {
      if (resourceKey === 'research' || resourceKey.endsWith('_research')) {
        total += Number(value);
      }
    }
    return total;
  }
  return source[key] ?? 0;
}

function shouldShowResource(
  key: string,
  incomeTotals: Record<string, number>,
  expenseTotals: Record<string, number>
): boolean {
  if (key === 'research') {
    const breakdown = getResearchBreakdown(incomeTotals);
    const totalIncome = breakdown.physics + breakdown.society + breakdown.engineering;
    const totalExpense = resolveResource(expenseTotals, key);
    return totalIncome !== 0 || totalExpense !== 0;
  }

  const incomeValue = resolveResource(incomeTotals, key);
  const expenseValue = resolveResource(expenseTotals, key);
  return incomeValue !== 0 || expenseValue !== 0;
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




