use Std.Ops.Drop;
use Std.Deref.Receiver;
use Std.Deref.Deref;
use Std.Deref.DerefMut;
use Std.Mem.ManuallyDrop;
use Std.Mem.SizeOf;
use Std.Mem.AlignOf;
use Std.Ptr.PtrWrite;
use Std.Ptr.PtrAsU8;
use Std.Ptr.DestructPtr;

fn Alloc(size: usize, align: usize) -> *mut u8;
fn Dealloc(ptr: *mut u8, size: usize, align: usize);
fn AllocZeroed(size: usize, align: usize) -> *mut u8;

struct Box(T:! type) {
    ptr: *mut T
}

impl[T:! type] Box(T) {
    fn New(val: T) -> Box(T) {
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        let raw: *mut u8 = Alloc(s, a);
        let typed: *mut T = PtrWrite(raw, val);
        make Box { ptr: typed }
    }

    fn Leak(b: Box(T)) -> &'static mut T {
        let ptr: *mut T = b.ptr;
        let _prevent_drop = ManuallyDrop.New(b);
        &mut *ptr
    }
}

impl[T:! type] Drop for Box(T) {
    fn Drop(self: &mut Self) {
        DestructPtr(self.ptr);
        let p: *mut u8 = PtrAsU8(self.ptr);
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        Dealloc(p, s, a);
    }
}

impl[T:! type] Receiver for Box(T) {
    let Target :! type = T;
}

impl[T:! type] Deref for Box(T) {
    fn deref(self: &Self) -> &T {
        &*self.ptr
    }
}

impl[T:! type] DerefMut for Box(T) {
    fn deref_mut(self: &mut Self) -> &mut T {
        &mut *self.ptr
    }
}
