trait Marker {}

impl Marker for i32 {
    type Bogus = bool;
}

fn main() -> i32 {
    5
}
