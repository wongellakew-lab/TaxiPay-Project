import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminStatisticsStore } from '../../../core/stores/admin-statistics.store';

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule],
  templateUrl: './admin-analytics.component.html',
  styleUrl: './admin-analytics.component.scss'
})
export class AdminAnalyticsComponent implements OnInit {
  public adminStatsStore = inject(AdminStatisticsStore);
  private snackBar = inject(MatSnackBar);

  get analytics() {
    return this.adminStatsStore.overview;
  }

  ngOnInit(): void {
    this.adminStatsStore.loadOverview();
  }

  exportReport(): void {
    const data = this.analytics();
    if (!data) return;

    let csvContent = 'data:text/csv;charset=utf-8,';
    csvContent += 'TAXIPAY FINANCIAL & TRANSIT ANALYTICS REPORT\n';
    csvContent += `Generated At: ${new Date().toISOString()}\n\n`;

    csvContent += 'SUMMARY METRICS\n';
    csvContent += `Total Platform Revenue (ETB),${data.totalRevenue}\n`;
    csvContent += `Today Revenue (ETB),${data.todayRevenue}\n`;
    csvContent += `Total Completed Trips,${data.totalTrips}\n`;
    csvContent += `Active Live Trips,${data.activeTrips}\n`;
    csvContent += `Total Accredited Drivers,${data.totalDrivers}\n`;
    csvContent += `Active Drivers On Shift,${data.activeDriversOnShift}\n`;
    csvContent += `Total Registered Passengers,${data.totalPassengers}\n`;
    csvContent += `Open Disputes,${data.openDisputes}\n\n`;

    csvContent += 'DAILY REVENUE BREAKDOWN (PAST 7 DAYS)\n';
    csvContent += 'Date,Trips Recorded,Gross Revenue (ETB),Platform Fee (1.5%),Driver Payout (98.5%)\n';
    data.dailyRevenueHistory.forEach(d => {
      csvContent += `"${d.date}",${d.tripsCount},${d.totalRevenue},${(d.totalRevenue * 0.015).toFixed(2)},${(d.totalRevenue * 0.985).toFixed(2)}\n`;
    });

    if (data.corridorStats?.length) {
      csvContent += '\nCORRIDOR PERFORMANCE\n';
      csvContent += 'Route Code,Corridor Name,Gross Revenue (ETB),Revenue Share (%),Active Minibuses,Total Trips\n';
      data.corridorStats.forEach(c => {
        csvContent += `"${c.routeCode}","${c.routeName}",${c.totalRevenue},${c.revenueSharePercent},${c.activeDriversCount},${c.totalTripsCount}\n`;
      });
    }

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `taxipay_analytics_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    this.snackBar.open('Official Financial & Transit CSV Report downloaded! 📊', 'OK', {
      duration: 3500,
      panelClass: 'snack-success'
    });
  }
}
