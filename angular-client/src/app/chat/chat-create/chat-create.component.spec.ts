import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChatCreateComponent } from './chat-create.component';

describe('ChatCreateComponent', () => {
  let component: ChatCreateComponent;
  let fixture: ComponentFixture<ChatCreateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ChatCreateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChatCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
