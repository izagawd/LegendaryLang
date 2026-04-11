use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;

struct Counter['a] { r: &'a mut i32 }
impl['a] Drop for Counter['a] {
    fn Drop(self: &mut Self) { *self.r = *self.r + 1; }
}

fn suppress[T:! type](val: T) {
    let _md = ManuallyDrop.New(val);
}

fn main() -> i32 {
    let counter = 0;
    {
        let c = make Counter { r: &mut counter };
        suppress(c);
    };
    counter
}
