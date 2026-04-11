use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;

struct Counter['a] { r: &'a mut i32 }
impl['a] Drop for Counter['a] {
    fn Drop(self: &mut Self) { *self.r = *self.r + 1; }
}

fn main() -> i32 {
    let counter = 0;
    {
        let c1 = make Counter { r: &mut counter };
        let c2 = make Counter { r: &mut counter };
        let _md1 = ManuallyDrop.New(c1);
        let _md2 = ManuallyDrop.New(c2);
    };
    counter
}
