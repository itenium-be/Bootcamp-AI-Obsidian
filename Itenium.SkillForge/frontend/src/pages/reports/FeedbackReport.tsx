import { useTranslation } from 'react-i18next';
import { MessageSquare, Star, BarChart3, PieChart, LineChart, Send } from 'lucide-react';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  CardDescription,
  Button,
  Badge,
} from '@itenium-forge/ui';
import { toast } from 'sonner';

function PlaceholderChart({
  label,
  icon: Icon,
  height = 'h-32',
}: {
  label: string;
  icon: React.ElementType;
  height?: string;
}) {
  return (
    <div
      className={`${height} rounded-lg border-2 border-dashed border-muted-foreground/20 flex flex-col items-center justify-center gap-2 bg-muted/10`}
    >
      <Icon className="size-8 text-muted-foreground/30" />
      <span className="text-xs text-muted-foreground/50 font-medium">{label}</span>
    </div>
  );
}

export function FeedbackReport() {
  const { t } = useTranslation();

  const handleSubmitIdea = () => {
    toast.success('Your feedback idea has been noted! Thank you.');
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('reports.feedback')}</h1>
          <p className="text-muted-foreground">{t('reports.feedbackSubtitle')}</p>
        </div>
        <Badge variant="secondary" className="text-sm px-3 py-1">
          Coming Soon
        </Badge>
      </div>

      {/* Coming Soon Alert */}
      <div className="flex items-start gap-3 rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-800 dark:bg-blue-950">
        <MessageSquare className="size-4 text-blue-600 dark:text-blue-400 mt-0.5 shrink-0" />
        <p className="text-sm text-blue-700 dark:text-blue-300">
          {t('reports.feedbackComingSoon')} — The feedback module will allow learners to rate
          courses and provide structured feedback after completion.
        </p>
      </div>

      {/* What's Coming Section */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Star className="size-5 text-amber-500" />
            What's Coming in Feedback Reports
          </CardTitle>
          <CardDescription>
            Here's a preview of the analytics you'll have access to once the feedback module is
            configured
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <Star className="size-4 text-amber-500" />
                <span className="text-sm font-medium">Average Course Ratings</span>
              </div>
              <p className="text-xs text-muted-foreground">
                5-star ratings per course with trend over time
              </p>
              <PlaceholderChart label="Star Rating Distribution" icon={BarChart3} />
            </div>

            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <PieChart className="size-4 text-blue-500" />
                <span className="text-sm font-medium">Satisfaction Breakdown</span>
              </div>
              <p className="text-xs text-muted-foreground">
                Satisfaction scores segmented by category and level
              </p>
              <PlaceholderChart label="Satisfaction Pie Chart" icon={PieChart} />
            </div>

            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <LineChart className="size-4 text-green-500" />
                <span className="text-sm font-medium">Feedback Trend</span>
              </div>
              <p className="text-xs text-muted-foreground">
                Monthly feedback volume and sentiment analysis
              </p>
              <PlaceholderChart label="Trend Over Time" icon={LineChart} />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Feature Preview Cards */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Planned Features</CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="space-y-2 text-sm">
              {[
                'Post-completion course surveys',
                '1-5 star rating system per course',
                'Net Promoter Score (NPS) tracking',
                'Open-ended feedback text analysis',
                'Manager digest: team satisfaction overview',
                'Export feedback data to CSV/PDF',
              ].map((feature) => (
                <li key={feature} className="flex items-center gap-2">
                  <div className="size-1.5 rounded-full bg-muted-foreground/40 flex-shrink-0" />
                  <span className="text-muted-foreground">{feature}</span>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>

        {/* CTA Card */}
        <Card className="bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-950 dark:to-pink-950">
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <Send className="size-4 text-purple-500" />
              Have a feature idea?
            </CardTitle>
            <CardDescription>
              Help us build the right feedback tools for your team. Share your ideas and we'll
              prioritize the most requested features.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div className="rounded-lg bg-background/60 border p-3">
                <p className="text-xs text-muted-foreground italic">
                  "I'd love to see anonymous feedback options so learners feel comfortable being
                  honest about course quality..."
                </p>
              </div>
              <Button onClick={handleSubmitIdea} className="w-full gap-2">
                <Send className="size-4" />
                Submit Feedback Idea
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
