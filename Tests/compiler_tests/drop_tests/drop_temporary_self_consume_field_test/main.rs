// make Pair { x: 20, y: 22 }.sum() where sum takes self: Self.
// Accesses both fields of the consumed value and returns their sum.
// Result: 20 + 22 = 42.

struct Pair {
    x: i32,
    y: i32
}

impl Pair {
    fn sum(self: Self) -> i32 {
        self.x + self.y
    }
}

fn main() -> i32 {
    make Pair { x: 20, y: 22 }.sum()
}
