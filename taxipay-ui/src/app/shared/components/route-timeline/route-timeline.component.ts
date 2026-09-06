import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NodeDto } from '../../../core/models/driver.models';

@Component({
  selector: 'app-route-timeline',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTooltipModule],
  templateUrl: './route-timeline.component.html',
  styleUrl: './route-timeline.component.scss'
})
export class RouteTimelineComponent implements OnChanges {
  @Input() routeName = '';
  @Input() routeCode = '';
  @Input() nodes: NodeDto[] = [];
  @Input() selectedFrom = '';
  @Input() selectedTo = '';
  @Input() isInteractive = false;

  @Output() fromSelected = new EventEmitter<string>();
  @Output() toSelected = new EventEmitter<string>();

  @ViewChild('timelineContainer') timelineContainerRef?: ElementRef<HTMLDivElement>;

  fromIdx = -1;
  toIdx = -1;

  trackLeftStyle = '0%';
  trackWidthStyle = '100%';
  progressLeftStyle = '0%';
  progressWidthStyle = '0%';

  private activeDragTarget: 'from' | 'to' | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    this.updateIndicesAndTrack();
  }

  get isValidSelection(): boolean {
    return this.fromIdx >= 0 && this.toIdx > this.fromIdx;
  }

  get selectionStatusText(): string {
    if (this.fromIdx < 0 || this.toIdx < 0) return 'Select boarding and alighting stops';
    if (this.fromIdx >= this.toIdx) return 'Boarding must be before alighting';
    return `Valid: ${this.toIdx - this.fromIdx} hop(s) selected`;
  }

  updateIndicesAndTrack(): void {
    if (!this.nodes || this.nodes.length === 0) {
      this.fromIdx = -1;
      this.toIdx = -1;
      this.trackLeftStyle = '0%';
      this.trackWidthStyle = '100%';
      this.progressLeftStyle = '0%';
      this.progressWidthStyle = '0%';
      return;
    }

    const n = this.nodes.length;
    this.fromIdx = this.nodes.findIndex(node => node.id === this.selectedFrom);
    this.toIdx = this.nodes.findIndex(node => node.id === this.selectedTo);

    if (n <= 1) {
      this.trackLeftStyle = '50%';
      this.trackWidthStyle = '0%';
      this.progressLeftStyle = '0%';
      this.progressWidthStyle = '0%';
      return;
    }

    // Connect exact centers of first stop (i=0) and last stop (i=n-1)
    this.trackLeftStyle = `calc(100% / (2 * ${n}))`;
    this.trackWidthStyle = `calc(100% * (${n - 1}) / ${n})`;

    if (this.fromIdx >= 0 && this.toIdx > this.fromIdx) {
      const fromPercent = (100 * this.fromIdx) / (n - 1);
      const spanPercent = (100 * (this.toIdx - this.fromIdx)) / (n - 1);
      this.progressLeftStyle = `${fromPercent}%`;
      this.progressWidthStyle = `${spanPercent}%`;
    } else {
      this.progressLeftStyle = '0%';
      this.progressWidthStyle = '0%';
    }
  }

  getMarkerPositionPercent(idx: number): string {
    if (!this.nodes || this.nodes.length <= 1 || idx < 0) return '0%';
    const n = this.nodes.length;
    const offset = 100 / (2 * n);
    const step = (100 * (n - 1)) / n / (n - 1);
    const pos = offset + idx * step;
    return `${pos}%`;
  }

  isInRange(index: number): boolean {
    if (this.fromIdx < 0 || this.toIdx < 0) return false;
    return index > this.fromIdx && index < this.toIdx;
  }

  onDragStart(event: PointerEvent, target: 'from' | 'to'): void {
    if (!this.isInteractive) return;
    event.preventDefault();
    this.activeDragTarget = target;

    const onPointerMove = (e: PointerEvent) => this.handleDragMove(e);
    const onPointerUp = () => {
      this.activeDragTarget = null;
      window.removeEventListener('pointermove', onPointerMove);
      window.removeEventListener('pointerup', onPointerUp);
    };

    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
  }

  private handleDragMove(event: PointerEvent): void {
    if (!this.activeDragTarget || !this.nodes || this.nodes.length < 2) return;
    const container = this.timelineContainerRef?.nativeElement;
    if (!container) return;

    const rect = container.getBoundingClientRect();
    const relativeX = Math.max(0, Math.min(rect.width, event.clientX - rect.left));
    const ratio = relativeX / rect.width;

    const n = this.nodes.length;
    const rawIndex = Math.round(ratio * (n - 1));
    const clampedIndex = Math.max(0, Math.min(n - 1, rawIndex));

    if (this.activeDragTarget === 'from') {
      // Boarding marker cannot be at or past toIdx
      const maxAllowed = this.toIdx > 0 ? this.toIdx - 1 : n - 2;
      const targetIdx = Math.min(clampedIndex, maxAllowed);
      if (targetIdx !== this.fromIdx && targetIdx >= 0) {
        this.selectedFrom = this.nodes[targetIdx].id;
        this.fromSelected.emit(this.nodes[targetIdx].id);
        this.updateIndicesAndTrack();
      }
    } else if (this.activeDragTarget === 'to') {
      // Alighting marker cannot be at or before fromIdx
      const minAllowed = this.fromIdx >= 0 ? this.fromIdx + 1 : 1;
      const targetIdx = Math.max(clampedIndex, minAllowed);
      if (targetIdx !== this.toIdx && targetIdx < n) {
        this.selectedTo = this.nodes[targetIdx].id;
        this.toSelected.emit(this.nodes[targetIdx].id);
        this.updateIndicesAndTrack();
      }
    }
  }

  onNodeClick(node: NodeDto, index: number): void {
    if (!this.isInteractive || !this.nodes || this.nodes.length === 0) return;
    const n = this.nodes.length;
    const fromIdx = this.fromIdx;
    const toIdx = this.toIdx;

    if (toIdx >= 0 && index > toIdx) {
      this.selectedTo = node.id;
      this.toSelected.emit(node.id);
    } else if (fromIdx >= 0 && index < fromIdx) {
      this.selectedFrom = node.id;
      this.fromSelected.emit(node.id);
    } else if (index === fromIdx || index === toIdx) {
      return;
    } else {
      if (index - fromIdx <= toIdx - index) {
        this.selectedFrom = node.id;
        this.fromSelected.emit(node.id);
      } else {
        this.selectedTo = node.id;
        this.toSelected.emit(node.id);
      }
    }
    this.updateIndicesAndTrack();
  }

  getNodeTooltip(node: NodeDto, index: number): string {
    return `Stop #${index + 1}: ${node.name} (Code: ${node.id})`;
  }
}
