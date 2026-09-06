import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { FareMatrixComponent } from './fare-matrix.component';

describe('FareMatrixComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FareMatrixComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();
  });

  it('should create and calculate initial default fare from Stop A to Stop F', () => {
    const fixture = TestBed.createComponent(FareMatrixComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.selectedFrom).toBe('A');
    expect(component.selectedTo).toBe('F');
    expect(component.computedFare()).toBe(25);
  });

  it('should allow changing boarding point and recalculate fare', () => {
    const fixture = TestBed.createComponent(FareMatrixComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onBoardingChange('B');
    expect(component.selectedFrom).toBe('B');
    expect(component.computedFare()).toBe(20);

    component.onBoardingChange('D');
    expect(component.selectedFrom).toBe('D');
    expect(component.computedFare()).toBe(10);
  });

  it('should allow changing destination point and recalculate fare', () => {
    const fixture = TestBed.createComponent(FareMatrixComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onDestinationChange('C');
    expect(component.selectedTo).toBe('C');
    expect(component.computedFare()).toBe(10);
  });

  it('should handle timeline node selection events', () => {
    const fixture = TestBed.createComponent(FareMatrixComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onTimelineFromSelected('C');
    expect(component.selectedFrom).toBe('C');
    expect(component.computedFare()).toBe(15);

    component.onTimelineToSelected('E');
    expect(component.selectedTo).toBe('E');
    expect(component.computedFare()).toBe(10);
  });
});
