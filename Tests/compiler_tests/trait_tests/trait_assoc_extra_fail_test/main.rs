trait Marker {}

impl Marker for i32 {
    let Bogus :! Sized = bool;
}

fn main() -> i32 {
    5
}
