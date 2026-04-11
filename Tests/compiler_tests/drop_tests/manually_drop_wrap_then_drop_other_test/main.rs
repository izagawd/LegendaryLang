use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;

struct Adder['a] { r: &'a mut i32, amount: i32 }
impl['a] Drop for Adder['a] {
    fn Drop(self: &mut Self) { *self.r = *self.r + self.amount; }
}

fn main() -> i32 {
    let counter = 0;
    {
        let suppressed = make Adder { r: &mut counter, amount: 100 };
        let _md = ManuallyDrop.New(suppressed);
        let active = make Adder { r: &mut counter, amount: 7 };
    };
    counter
}
