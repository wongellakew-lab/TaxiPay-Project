import { Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { PayTripResponse } from '../../../core/models/passenger.models';
import * as QRCode from 'qrcode';

@Component({
  selector: 'app-receipt-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './receipt-dialog.component.html',
  styleUrl: './receipt-dialog.component.scss'
})
export class ReceiptDialogComponent implements OnInit {
  @ViewChild('qrCanvas', { static: true }) qrCanvas!: ElementRef<HTMLCanvasElement>;

  constructor(
    @Inject(MAT_DIALOG_DATA) public receipt: PayTripResponse,
    private dialogRef: MatDialogRef<ReceiptDialogComponent>
  ) {}

  ngOnInit(): void {
    this.generateQrCode();
  }

  private generateQrCode(): void {
    const dataString = JSON.stringify({
      receipt: this.receipt.receiptNumber,
      driver: this.receipt.driverId,
      fare: this.receipt.fare,
      route: this.receipt.routeName,
      date: this.receipt.paidAt
    });

    QRCode.toCanvas(this.qrCanvas.nativeElement, dataString, {
      width: 120,
      margin: 1,
      color: {
        dark: '#0f172a',
        light: '#ffffff'
      }
    }).catch(err => console.error('QR generation error', err));
  }

  printReceipt(): void {
    window.print();
  }

  close(): void {
    this.dialogRef.close();
  }
}
