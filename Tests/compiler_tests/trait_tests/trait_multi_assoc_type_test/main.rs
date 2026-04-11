trait Transform {
    let Input :! Sized;
    let Output :! Sized;
    fn transform(x: i32) -> i32;
}

impl Transform for i32 {
    let Input :! Sized = bool;
    let Output :! Sized = i32;
    fn transform(x: i32) -> i32 {
        x + 1
    }
}

fn main() -> i32 {
    (i32 as Transform).transform(9)
}
