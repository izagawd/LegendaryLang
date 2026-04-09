trait Transform {
    let Input :! type;
    let Output :! type;
    fn transform(x: i32) -> i32;
}

impl Transform for i32 {
    let Input :! type = bool;
    let Output :! type = i32;
    fn transform(x: i32) -> i32 {
        x + 1
    }
}

fn main() -> i32 {
    (i32 as Transform).transform(9)
}
