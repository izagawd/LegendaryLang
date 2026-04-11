use Std.Deref.Receiver;
use Std.Deref.Deref;
use Std.Deref.DerefMut;
use Std.Mem.SizeOf;
use Std.Mem.AlignOf;
use Std.Ptr.PtrWrite;
use Std.Ptr.PtrAsU8;
use Std.Ptr.DestructPtr;

fn Alloc(size: usize, align: usize) -> *mut u8;
fn Dealloc(ptr: *mut u8, size: usize, align: usize);
fn AllocZeroed(size: usize, align: usize) -> *mut u8;

struct GcMut(T:! type) {
    ptr: *mut T
}

impl[T:! Sized] GcMut(T) {
    fn New(val: T) -> GcMut(T) {
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        let raw: *mut u8 = Alloc(s, a);
        let typed: *mut T = PtrWrite(raw, val);
        make GcMut { ptr: typed }
    }
}

impl[T:! type] Copy for GcMut(T) {}

impl[T:! type] Receiver for GcMut(T) {
    let Target :! type = T;
}

impl[T:! type] Deref for GcMut(T) {
    fn deref(self: &Self) -> &T {
        &*self.ptr
    }
}

impl[T:! type] DerefMut for GcMut(T) {
    fn deref_mut(self: &mut Self) -> &mut T {
        &mut *self.ptr
    }
}
