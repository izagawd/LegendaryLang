use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;

struct Counter['a] { r: &'a mut i32 }
impl['a] Drop for Counter['a] {
    fn Drop(self: &mut Self) { *self.r = *self.r + 1; }
}

fn main() -> i32 {
    let a = 0;
    let b = 0;
    {
        let c1 = make Counter { r: &mut a };
        let _md = ManuallyDrop.New(c1);
        let c2 = make Counter { r: &mut b };
    };
    a + b
}
