trait Addable: Sized {
    fn Add(a: Self, b: Self) -> Self;
}

impl Addable for i32 {
    fn Add(a: i32, b: i32) -> i32 {
        a + b
    }
}

struct Point {
    x: i32,
    y: i32
}

trait HasValue: Sized {
    fn get_value(self_val: Self) -> i32;
}

impl HasValue for Point {
    fn get_value(self_val: Point) -> i32 {
        self_val.x + self_val.y
    }
}

fn add_things(T:! Addable, a: T, b: T) -> T {
    Addable.Add(a, b)
}

fn get_val(T:! HasValue, thing: T) -> i32 {
    HasValue.get_value(thing)
}

fn main() -> i32 {
    let sum = add_things(i32, 10, 20);
    let p = make Point { x : 3, y : 7 };
    let pval = get_val(Point, p);
    sum + pval
}
