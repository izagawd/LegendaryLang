use Std.Core.Marker.Drop;
struct Dropper['a] {
    r: &'a uniq i32,
    amount: i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + self.amount;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let d = make Dropper { r : &uniq counter, amount : 7 };
    }
    counter
}
