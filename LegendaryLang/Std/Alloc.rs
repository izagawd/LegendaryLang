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

struct Gc(T:! type) {
    ptr: *mut T
}

impl[T:! type] Gc(T) {
    fn New(val: T) -> Gc(T) {
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        let raw: *mut u8 = Alloc(s, a);
        let typed: *mut T = PtrWrite(raw, val);
        make Gc { ptr: typed }
    }
}

impl[T:! MetaSized] Copy for Gc(T) {}

impl[T:! MetaSized] Receiver for Gc(T) {
    let Target :! type = T;
}

impl[T:! MetaSized] Deref for Gc(T) {
    fn deref(self: &Self) -> &T {
        &*self.ptr
    }
}

impl[T:! MetaSized] DerefMut for Gc(T) {
    fn deref_mut(self: &mut Self) -> &mut T {
        &mut *self.ptr
    }
}
