struct Pair {
    x: i32,
    y: i32
}
impl Copy for Pair {}

impl Pair {
    fn get_x(self: &Self) -> i32 {
        self.x
    }
    fn get_y(self: &Self) -> i32 {
        self.y
    }
}

fn main() -> i32 {
    let b: GcMut(Pair) = GcMut.New(make Pair { x: 10, y: 32 });
    b.get_x() + b.get_y()
}
