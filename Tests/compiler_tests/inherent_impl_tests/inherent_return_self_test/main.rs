struct Point {
    x: i32,
    y: i32
}

impl Copy for Point {}

impl Point {
    fn new(x: i32, y: i32) -> Self {
        make Point { x : x, y : y }
    }

    fn sum(self: &Self) -> i32 {
        self.x + self.y
    }
}

fn main() -> i32 {
    let p = Point.new(3, 7);
    p.sum()
}
