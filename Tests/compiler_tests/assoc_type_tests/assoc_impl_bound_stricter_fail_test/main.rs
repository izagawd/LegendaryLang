trait Producer: Sized {
    let Item :! type;
}

impl Producer for i32 {
    let Item :! Sized = i32;
}

fn main() -> i32 { 0 }
