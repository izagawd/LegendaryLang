use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32,
    amount: i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + self.amount;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let d = make Dropper { r : &mut counter, amount : 7 };
    }
    counter
}
