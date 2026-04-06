use Std.Ops.Drop;
use Std.Deref.Receiver;
use Std.Deref.Deref;
use Std.Deref.DerefConst;
use Std.Deref.DerefMut;
use Std.Deref.DerefUniq;
use Std.Mem.ManuallyDrop;
use Std.Mem.SizeOf;
use Std.Mem.AlignOf;
use Std.Ptr.PtrWrite;
use Std.Ptr.PtrAsU8;
use Std.Ptr.DestructPtr;

fn Alloc(size: usize, align: usize) -> *uniq u8;
fn Dealloc(ptr: *uniq u8, size: usize, align: usize);
fn AllocZeroed(size: usize, align: usize) -> *uniq u8;

struct Box(T:! type) {
    ptr: *uniq T
}

impl[T:! type] Box(T) {
    fn New(val: T) -> Box(T) {
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        let raw: *uniq u8 = Alloc(s, a);
        let typed: *uniq T = PtrWrite(raw, val);
        make Box { ptr: typed }
    }

    fn Leak(b: Box(T)) -> &'static uniq T {
        let ptr: *uniq T = b.ptr;
        let _prevent_drop = make ManuallyDrop(Box(T)) { val: b };
        &uniq *ptr
    }
}

impl[T:! type] Drop for Box(T) {
    fn Drop(self: &uniq Self) {
        DestructPtr(self.ptr);
        let p: *uniq u8 = PtrAsU8(self.ptr);
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        Dealloc(p, s, a);
    }
}

impl[T:! type] Receiver for Box(T) {
    type Target = T;
}

impl[T:! type] Deref for Box(T) {
    fn deref(self: &Self) -> &T {
        &*self.ptr
    }
}

impl[T:! type] DerefConst for Box(T) {
    fn deref_const(self: &const Self) -> &const T {
        &const *self.ptr
    }
}

impl[T:! type] DerefMut for Box(T) {
    fn deref_mut(self: &mut Self) -> &mut T {
        &mut *self.ptr
    }
}

impl[T:! type] DerefUniq for Box(T) {
    fn deref_uniq(self: &uniq Self) -> &uniq T {
        &uniq *self.ptr
    }
}
